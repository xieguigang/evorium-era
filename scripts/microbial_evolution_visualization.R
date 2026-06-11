#!/usr/bin/env Rscript
# =============================================================================
# Microbial Community Evolution Visualization
# =============================================================================
# This script reads cell snapshot JSON files from a folder and generates
# publication-quality figures for genome evolution analysis.
#
# Input:  A folder containing JSON files named like iter_00000099.json
#         Each JSON file is an array of cell objects at a given time point.
# Output: PDF and PNG figures saved to the output directory
#
# Usage:
#   Rscript microbial_evolution_visualization.R <input_dir> [output_dir]
#
# If output_dir is not specified, results are saved to <input_dir>/figures/
#
# Required R packages:
#   jsonlite, ggplot2, ape, ggtree, tidyr, dplyr, tibble, purrr,
#   reshape2, viridis, patchwork, ggridges, scales, ggnewscale
#
# Install missing packages with:
#   install.packages(c("jsonlite","ggplot2","ape","ggtree","tidyr","dplyr",
#     "tibble","purrr","reshape2","viridis","patchwork","ggridges","scales",
#     "ggnewscale"))
# =============================================================================

# ---- 0. Setup and Parameter Parsing -----------------------------------------
args <- commandArgs(trailingOnly = TRUE)
if (length(args) < 1) {
  cat("Usage: Rscript microbial_evolution_visualization.R <input_dir> [output_dir]\n")
  quit(status = 1)
}

INPUT_DIR  <- args[1]
OUTPUT_DIR <- ifelse(length(args) >= 2, args[2], file.path(INPUT_DIR, "figures"))

if (!dir.exists(OUTPUT_DIR)) {
  dir.create(OUTPUT_DIR, recursive = TRUE)
}

cat(sprintf("[INFO] Input directory : %s\n", INPUT_DIR))
cat(sprintf("[INFO] Output directory: %s\n", OUTPUT_DIR))

# ---- 1. Load Required Packages ----------------------------------------------
# Auto-install missing packages
required_pkgs <- c("jsonlite","ggplot2","ape","ggtree","tidyr","dplyr",
                   "tibble","purrr","reshape2","viridis","patchwork",
                   "ggridges","scales","ggnewscale")

for (pkg in required_pkgs) {
  if (!requireNamespace(pkg, quietly = TRUE)) {
    cat(sprintf("[INSTALL] Installing package: %s\n", pkg))
    if (pkg == "ggtree") {
      if (!requireNamespace("BiocManager", quietly = TRUE)) {
        install.packages("BiocManager", repos = "https://cloud.r-project.org")
      }
      BiocManager::install("ggtree", ask = FALSE, update = FALSE)
    } else {
      install.packages(pkg, repos = "https://cloud.r-project.org")
    }
  }
}

suppressPackageStartupMessages({
  library(jsonlite)
  library(ggplot2)
  library(ape)
  library(ggtree)
  library(tidyr)
  library(dplyr)
  library(tibble)
  library(purrr)
  library(reshape2)
  library(viridis)
  library(patchwork)
  library(ggridges)
  library(scales)
  library(ggnewscale)
})

# Publication-quality theme
theme_pub <- function(base_size = 11) {
  theme_bw(base_size = base_size) +
    theme(
      panel.grid.minor = element_blank(),
      panel.border     = element_rect(colour = "black", linewidth = 0.6),
      axis.text        = element_text(colour = "black", size = base_size),
      axis.title       = element_text(colour = "black", size = base_size + 1),
      legend.text      = element_text(size = base_size - 1),
      legend.title     = element_text(size = base_size),
      strip.text       = element_text(size = base_size),
      strip.background = element_rect(fill = "grey92", colour = "black", linewidth = 0.4),
      plot.title       = element_text(hjust = 0.5, size = base_size + 2, face = "bold"),
      plot.subtitle    = element_text(hjust = 0.5, size = base_size)
    )
}

# ---- 2. Data Loading Functions ----------------------------------------------

#' Parse a single JSON snapshot file and return a flattened data frame
parse_snapshot <- function(filepath) {
  raw <- tryCatch(fromJSON(filepath, flatten = FALSE), error = function(e) NULL)
  if (is.null(raw) || length(raw) == 0) return(NULL)

  # Extract time point from filename (e.g., iter_00000099.json -> 99)
  basename_file <- basename(filepath)
  time_point <- as.integer(gsub("[^0-9]", "", tools::file_path_sans_ext(basename_file)))

  rows <- lapply(raw, function(cell) {
    # Extract gene list from genome
    genome_genes <- if (!is.null(cell$Genome$Genes) && length(cell$Genome$Genes) > 0) {
      cell$Genome$Genes
    } else {
      character(0)
    }

    # Extract plasmid genes
    plasmid_genes <- if (!is.null(cell$Plasmids) && length(cell$Plasmids) > 0) {
      unlist(lapply(cell$Plasmids, function(p) {
        if (!is.null(p$Genes) && length(p$Genes) > 0) p$Genes else character(0)
      }))
    } else {
      character(0)
    }

    # All genes (genome + plasmids)
    all_genes <- c(genome_genes, plasmid_genes)

    # Gene counts as a collapsed string for later expansion
    gene_counts_str <- if (!is.null(cell$GeneCounts) && length(cell$GeneCounts) > 0) {
      paste(names(cell$GeneCounts), unlist(cell$GeneCounts), sep = ":", collapse = ",")
    } else {
      ""
    }

    # Internal molecules as collapsed string
    molecules_str <- if (!is.null(cell$InternalMolecules) && length(cell$InternalMolecules) > 0) {
      paste(names(cell$InternalMolecules), unlist(cell$InternalMolecules), sep = ":", collapse = ",")
    } else {
      ""
    }

    data.frame(
      TimePoint        = time_point,
      CellID           = cell$ID,
      ParentID         = if (is.null(cell$ParentID)) NA_character_ else as.character(cell$ParentID),
      Generation       = cell$Generation,
      IsAlive          = cell$IsAlive,
      Age              = cell$Age,
      GenomeSize       = cell$GenomeSize,
      HasCellWall      = cell$HasCellWall,
      ATP              = cell$ATP,
      PH               = cell$PH,
      DivisionCount    = cell$DivisionCount,
      PlasmidCount     = cell$PlasmidCount,
      PosX             = cell$Position$X,
      PosY             = cell$Position$Y,
      PosZ             = cell$Position$Z,
      OsmoticState     = cell$OsmoticState,
      TotalMolecules   = cell$TotalMolecules,
      InternalIonStr   = cell$InternalIonStrength,
      NGenomeGenes     = length(genome_genes),
      NPlasmidGenes    = length(plasmid_genes),
      NTotalGenes      = length(all_genes),
      GenomeGenesStr   = paste(genome_genes, collapse = ","),
      PlasmidGenesStr  = paste(plasmid_genes, collapse = ","),
      AllGenesStr      = paste(all_genes, collapse = ","),
      GeneCountsStr    = gene_counts_str,
      MoleculesStr     = molecules_str,
      stringsAsFactors = FALSE
    )
  })

  do.call(rbind, rows)
}

#' Expand gene counts from collapsed string to a data frame
expand_gene_counts <- function(gene_counts_str, cell_id, time_point) {
  if (is.na(gene_counts_str) || gene_counts_str == "") {
    return(data.frame(TimePoint = time_point, CellID = cell_id,
                      Gene = NA_character_, Count = NA_integer_,
                      stringsAsFactors = FALSE))
  }
  pairs <- strsplit(gene_counts_str, ",")[[1]]
  result <- lapply(pairs, function(p) {
    parts <- strsplit(p, ":")[[1]]
    data.frame(TimePoint = time_point, CellID = cell_id,
               Gene = parts[1], Count = as.integer(parts[2]),
               stringsAsFactors = FALSE)
  })
  do.call(rbind, result)
}

#' Expand internal molecules from collapsed string
expand_molecules <- function(molecules_str, cell_id, time_point) {
  if (is.na(molecules_str) || molecules_str == "") {
    return(data.frame(TimePoint = time_point, CellID = cell_id,
                      Molecule = NA_character_, Amount = NA_integer_,
                      stringsAsFactors = FALSE))
  }
  pairs <- strsplit(molecules_str, ",")[[1]]
  result <- lapply(pairs, function(p) {
    parts <- strsplit(p, ":")[[1]]
    data.frame(TimePoint = time_point, CellID = cell_id,
               Molecule = parts[1], Amount = as.integer(parts[2]),
               stringsAsFactors = FALSE)
  })
  do.call(rbind, result)
}

# ---- 3. Load All Snapshot Data -----------------------------------------------
cat("[INFO] Loading snapshot files...\n")

json_files <- list.files(INPUT_DIR, pattern = "^iter_.*\\.json$", full.names = TRUE)
json_files <- sort(json_files)

if (length(json_files) == 0) {
  stop("[ERROR] No JSON files matching 'iter_*.json' found in the input directory.")
}

cat(sprintf("[INFO] Found %d snapshot files.\n", length(json_files)))

# Parse all snapshots
all_data <- lapply(json_files, function(f) {
  cat(sprintf("  Loading: %s\n", basename(f)))
  parse_snapshot(f)
})
cells_df <- do.call(rbind, all_data[!sapply(all_data, is.null)])

if (is.null(cells_df) || nrow(cells_df) == 0) {
  stop("[ERROR] No cell data could be parsed from the JSON files.")
}

cat(sprintf("[INFO] Total cell records: %d\n", nrow(cells_df)))
cat(sprintf("[INFO] Time range: %d - %d\n", min(cells_df$TimePoint), max(cells_df$TimePoint)))
cat(sprintf("[INFO] Unique cells : %d\n", length(unique(cells_df$CellID))))
cat(sprintf("[INFO] Generation range: %d - %d\n", min(cells_df$Generation), max(cells_df$Generation)))

# Expand gene counts
cat("[INFO] Expanding gene count data...\n")
gene_counts_list <- lapply(1:nrow(cells_df), function(i) {
  expand_gene_counts(cells_df$GeneCountsStr[i], cells_df$CellID[i], cells_df$TimePoint[i])
})
gene_counts_df <- do.call(rbind, gene_counts_list)
gene_counts_df <- gene_counts_df[!is.na(gene_counts_df$Gene), ]

# Expand molecules
cat("[INFO] Expanding molecule data...\n")
molecules_list <- lapply(1:nrow(cells_df), function(i) {
  expand_molecules(cells_df$MoleculesStr[i], cells_df$CellID[i], cells_df$TimePoint[i])
})
molecules_df <- do.call(rbind, molecules_list)
molecules_df <- molecules_df[!is.na(molecules_df$Molecule), ]

# ---- 4. Derived Data Computations -------------------------------------------

#' Compute pairwise genome Jaccard distance for cells at a given time point
compute_genome_distance <- function(cells_at_tp) {
  if (nrow(cells_at_tp) <= 1) return(NA)

  gene_sets <- strsplit(cells_at_tp$AllGenesStr, ",")
  names(gene_sets) <- cells_at_tp$CellID

  ids <- cells_at_tp$CellID
  n <- length(ids)
  dist_mat <- matrix(0, nrow = n, ncol = n)

  for (i in 1:(n - 1)) {
    for (j in (i + 1):n) {
      set_i <- gene_sets[[ids[i]]]
      set_j <- gene_sets[[ids[j]]]
      # Handle empty gene sets
      if (length(set_i) == 0 && length(set_j) == 0) {
        dist_mat[i, j] <- 0
      } else if (length(set_i) == 0 || length(set_j) == 0) {
        dist_mat[i, j] <- 1
      } else {
        intersection <- length(intersect(set_i, set_j))
        union_val    <- length(union(set_i, set_j))
        dist_mat[i, j] <- 1 - intersection / max(union_val, 1)
      }
      dist_mat[j, i] <- dist_mat[i, j]
    }
  }
  rownames(dist_mat) <- ids
  colnames(dist_mat) <- ids
  as.dist(dist_mat)
}

# Compute genomic diversity metrics per time point
cat("[INFO] Computing genomic diversity metrics...\n")

diversity_metrics <- cells_df %>%
  group_by(TimePoint) %>%
  summarise(
    NCells           = n(),
    NAlive           = sum(IsAlive),
    MeanGenomeSize   = mean(GenomeSize, na.rm = TRUE),
    SDGenomeSize     = sd(GenomeSize, na.rm = TRUE),
    MedianGenomeSize = median(GenomeSize, na.rm = TRUE),
    MeanGeneration   = mean(Generation, na.rm = TRUE),
    SDGeneration     = sd(Generation, na.rm = TRUE),
    MaxGeneration    = max(Generation, na.rm = TRUE),
    MeanPlasmidCount = mean(PlasmidCount, na.rm = TRUE),
    MeanNGenes       = mean(NTotalGenes, na.rm = TRUE),
    SDNGenes         = sd(NTotalGenes, na.rm = TRUE),
    PropCellWall     = mean(HasCellWall, na.rm = TRUE),
    MeanPH           = mean(PH, na.rm = TRUE),
    SDPH             = sd(PH, na.rm = TRUE),
    MeanATP          = mean(ATP, na.rm = TRUE),
    MeanAge          = mean(Age, na.rm = TRUE),
    .groups = "drop"
  )

# Compute mean pairwise Jaccard distance per time point
cat("[INFO] Computing pairwise Jaccard distances...\n")
mean_jaccard_by_tp <- cells_df %>%
  group_by(TimePoint) %>%
  group_split() %>%
  lapply(function(df) {
    if (nrow(df) < 2) {
      return(data.frame(TimePoint = df$TimePoint[1], MeanJaccard = NA_real_,
                        SDJaccard = NA_real_))
    }
    d <- compute_genome_distance(df)
    vals <- as.vector(d)
    vals <- vals[!is.na(vals) & is.finite(vals)]
    data.frame(TimePoint = df$TimePoint[1],
               MeanJaccard = if (length(vals) > 0) mean(vals) else NA_real_,
               SDJaccard   = if (length(vals) > 0) sd(vals) else NA_real_)
  }) %>%
  bind_rows()

diversity_metrics <- left_join(diversity_metrics, mean_jaccard_by_tp, by = "TimePoint")

# Gene frequency per time point
gene_freq <- gene_counts_df %>%
  group_by(TimePoint, Gene) %>%
  summarise(
    TotalCount    = sum(Count, na.rm = TRUE),
    MeanCount     = mean(Count, na.rm = TRUE),
    CellsWithGene = sum(Count > 0),
    .groups = "drop"
  )

# ---- 5. Visualization Helper ------------------------------------------------

#' Save a ggplot object to both PDF and PNG
save_figure <- function(p, name, width = 10, height = 8, dpi = 300) {
  pdf_path <- file.path(OUTPUT_DIR, paste0(name, ".pdf"))
  png_path <- file.path(OUTPUT_DIR, paste0(name, ".png"))

  ggsave(pdf_path, plot = p, width = width, height = height, device = cairo_pdf)
  ggsave(png_path, plot = p, width = width, height = height, dpi = dpi)

  cat(sprintf("[SAVED] %s\n", pdf_path))
  cat(sprintf("[SAVED] %s\n", png_path))
}

# =============================================================================
# FIGURE 1: Phylogenetic Tree of Cell Lineages
# =============================================================================
cat("\n[INFO] Generating Figure 1: Phylogenetic Tree...\n")

build_phylo_from_parent <- function(df) {
  # Collect all unique cell IDs
  all_ids    <- unique(df$CellID)
  parent_ids <- unique(df$ParentID[!is.na(df$ParentID)])

  # Root cells: have no parent (ParentID is NA) and Generation == 0
  root_ids <- unique(df$CellID[is.na(df$ParentID) & df$Generation == 0])

  # Build edge list from parent-child relationships
  edges <- df %>%
    filter(!is.na(ParentID)) %>%
    select(ParentID, CellID) %>%
    distinct()

  if (nrow(edges) == 0) {
    cat("[WARN] No parent-child edges found. Cannot build tree.\n")
    return(NULL)
  }

  # All nodes
  all_nodes <- unique(c(edges$ParentID, edges$CellID, root_ids))

  # Identify tips (nodes that are not parents of anyone)
  parent_set <- unique(edges$ParentID)
  tip_ids    <- setdiff(all_nodes, parent_set)
  int_ids    <- intersect(all_nodes, parent_set)

  # If root is also a tip, keep it as internal
  if (length(root_ids) > 0) {
    int_ids <- unique(c(int_ids, root_ids))
    tip_ids <- setdiff(tip_ids, root_ids)
  }

  n_tips     <- length(tip_ids)
  n_internal <- length(int_ids)

  if (n_tips == 0) {
    cat("[WARN] No tip nodes found. Cannot build tree.\n")
    return(NULL)
  }

  # Index mapping: tips = 1..n_tips, internal = (n_tips+1)..(n_tips+n_internal)
  tip_idx <- setNames(1:n_tips, tip_ids)
  int_idx <- setNames((n_tips + 1):(n_tips + n_internal), int_ids)
  all_idx <- c(tip_idx, int_idx)

  # Root node index
  root_node <- if (length(root_ids) > 0) all_idx[as.character(root_ids[1])] else (n_tips + 1)

  # Build edge matrix
  edge_mat <- matrix(0, nrow = nrow(edges), ncol = 2)
  for (i in 1:nrow(edges)) {
    pid <- as.character(edges$ParentID[i])
    cid <- as.character(edges$CellID[i])
    if (pid %in% names(all_idx) && cid %in% names(all_idx)) {
      edge_mat[i, 1] <- all_idx[pid]
      edge_mat[i, 2] <- all_idx[cid]
    }
  }

  # Remove edges with zero indices (shouldn't happen, but safety check)
  valid <- edge_mat[, 1] > 0 & edge_mat[, 2] > 0
  edge_mat <- edge_mat[valid, , drop = FALSE]

  if (nrow(edge_mat) == 0) {
    cat("[WARN] No valid edges for tree construction.\n")
    return(NULL)
  }

  # Compute branch lengths from generation difference
  edge_length <- rep(1, nrow(edge_mat))
  gen_lookup <- setNames(df$Generation, df$CellID)
  for (i in 1:nrow(edge_mat)) {
    pid <- as.character(edges$ParentID[i])
    cid <- as.character(edges$CellID[i])
    pg <- gen_lookup[pid]
    cg <- gen_lookup[cid]
    if (!is.na(pg) && !is.na(cg)) {
      edge_length[i] <- max(cg - pg, 0.5)
    }
  }

  # Create phylo object
  phy <- list(
    edge       = edge_mat,
    tip.label  = tip_ids,
    Nnode      = n_internal,
    node.label = int_ids,
    edge.length = edge_length
  )
  class(phy) <- "phylo"
  phy <- reorder(phy, "cladewise")

  phy
}

phy_tree <- tryCatch({
  build_phylo_from_parent(cells_df)
}, error = function(e) {
  cat(sprintf("[WARN] Could not build phylogenetic tree: %s\n", e$message))
  NULL
})

if (!is.null(phy_tree) && inherits(phy_tree, "phylo") && nrow(phy_tree$edge) > 0) {
  # Create annotation data for the tree
  tree_anno <- cells_df %>%
    select(CellID, Generation, TimePoint, GenomeSize, NTotalGenes, PlasmidCount, IsAlive) %>%
    distinct(CellID, .keep_all = TRUE)

  # Tip annotation
  tip_anno <- tree_anno[tree_anno$CellID %in% phy_tree$tip.label, ]
  rownames(tip_anno) <- tip_anno$CellID

  # Node annotation (internal nodes)
  node_anno <- tree_anno[tree_anno$CellID %in% phy_tree$node.label, ]
  if (nrow(node_anno) > 0) {
    rownames(node_anno) <- node_anno$CellID
  }

  # --- Rectangular phylogenetic tree ---
  p_tree <- ggtree(phy_tree, layout = "rectangular", size = 0.3, color = "grey40") +
    theme_tree2() +
    labs(title = "Phylogenetic Tree of Cell Lineages")

  if (nrow(tip_anno) > 0) {
    p_tree <- p_tree %<+% tip_anno +
      geom_tippoint(aes(color = Generation), size = 1.5, alpha = 0.8) +
      scale_color_viridis_c(option = "C", name = "Generation")
  }

  if (nrow(node_anno) > 0) {
    p_tree <- p_tree %<+% node_anno +
      geom_nodepoint(aes(color = Generation), size = 2, alpha = 0.6)
  }

  p_tree <- p_tree +
    theme(
      legend.position = "right",
      plot.title = element_text(hjust = 0.5, face = "bold", size = 14)
    )

  save_figure(p_tree, "fig1_phylogenetic_tree", width = 14, height = 10)

  # --- Circular phylogenetic tree ---
  p_tree_circ <- ggtree(phy_tree, layout = "circular", size = 0.3, color = "grey40") +
    labs(title = "Circular Phylogenetic Tree of Cell Lineages")

  if (nrow(tip_anno) > 0) {
    p_tree_circ <- p_tree_circ %<+% tip_anno +
      geom_tippoint(aes(color = Generation), size = 1.2, alpha = 0.8) +
      scale_color_viridis_c(option = "C", name = "Generation")
  }

  p_tree_circ <- p_tree_circ +
    theme(
      legend.position = "right",
      plot.title = element_text(hjust = 0.5, face = "bold", size = 14)
    )

  save_figure(p_tree_circ, "fig1b_phylogenetic_tree_circular", width = 12, height = 12)

  # --- Tree colored by genome size ---
  p_tree_gs <- ggtree(phy_tree, layout = "rectangular", size = 0.3, color = "grey40") +
    theme_tree2() +
    labs(title = "Phylogenetic Tree Colored by Genome Size")

  if (nrow(tip_anno) > 0) {
    p_tree_gs <- p_tree_gs %<+% tip_anno +
      geom_tippoint(aes(color = GenomeSize), size = 1.5, alpha = 0.8) +
      scale_color_viridis_c(option = "B", name = "Genome Size (bp)")
  }

  p_tree_gs <- p_tree_gs +
    theme(
      legend.position = "right",
      plot.title = element_text(hjust = 0.5, face = "bold", size = 14)
    )

  save_figure(p_tree_gs, "fig1c_phylogenetic_tree_genome_size", width = 14, height = 10)

} else {
  cat("[WARN] Skipping phylogenetic tree figures (insufficient lineage data).\n")
}

# =============================================================================
# FIGURE 2: Genome Size Evolution Over Time
# =============================================================================
cat("\n[INFO] Generating Figure 2: Genome Size Evolution...\n")

# Violin + boxplot
p_genome_size <- ggplot(cells_df, aes(x = factor(TimePoint), y = GenomeSize)) +
  geom_violin(aes(fill = factor(TimePoint)), alpha = 0.4, show.legend = FALSE, linewidth = 0.3) +
  geom_boxplot(width = 0.15, outlier.size = 0.5, linewidth = 0.3, fill = "white") +
  scale_fill_viridis_d(option = "D", begin = 0.2, end = 0.9) +
  labs(
    title = "Genome Size Distribution Across Evolutionary Time",
    x     = "Time Point (Iteration)",
    y     = "Genome Size (bp)"
  ) +
  theme_pub() +
  theme(
    axis.text.x = element_text(angle = 45, hjust = 1),
    plot.title  = element_text(hjust = 0.5, face = "bold")
  )

save_figure(p_genome_size, "fig2_genome_size_evolution", width = 12, height = 7)

# Trend line with confidence interval
p_genome_trend <- ggplot(diversity_metrics, aes(x = TimePoint)) +
  geom_ribbon(aes(ymin = MeanGenomeSize - SDGenomeSize,
                  ymax = MeanGenomeSize + SDGenomeSize),
              fill = "steelblue", alpha = 0.2) +
  geom_line(aes(y = MeanGenomeSize), color = "steelblue", linewidth = 1) +
  geom_point(aes(y = MeanGenomeSize), color = "steelblue", size = 2) +
  labs(
    title = "Mean Genome Size Trajectory Over Evolutionary Time",
    subtitle = "Shaded area represents mean +/- 1 SD",
    x = "Time Point (Iteration)",
    y = "Mean Genome Size (bp)"
  ) +
  theme_pub() +
  theme(plot.title = element_text(hjust = 0.5, face = "bold"),
        plot.subtitle = element_text(hjust = 0.5))

save_figure(p_genome_trend, "fig2b_genome_size_trend", width = 10, height = 6)

# =============================================================================
# FIGURE 3: Gene Frequency Dynamics Over Time
# =============================================================================
cat("\n[INFO] Generating Figure 3: Gene Frequency Dynamics...\n")

# Gene copy number heatmap
gene_freq_heatmap_data <- gene_counts_df %>%
  group_by(TimePoint, Gene) %>%
  summarise(MeanCount = mean(Count, na.rm = TRUE), .groups = "drop")

gene_freq_matrix <- acast(gene_freq_heatmap_data, Gene ~ TimePoint, value.var = "MeanCount", fill = 0)

p_gene_heatmap <- ggplot(melt(gene_freq_matrix), aes(x = factor(Var2), y = Var1, fill = value)) +
  geom_tile(color = "white", linewidth = 0.3) +
  scale_fill_viridis_c(option = "A", name = "Mean\nGene Copy\nNumber") +
  labs(
    title = "Gene Copy Number Heatmap Across Evolutionary Time",
    x = "Time Point (Iteration)",
    y = "Gene"
  ) +
  theme_pub() +
  theme(
    axis.text.y  = element_text(size = 8),
    axis.text.x  = element_text(angle = 45, hjust = 1),
    plot.title   = element_text(hjust = 0.5, face = "bold"),
    legend.position = "right"
  )

save_figure(p_gene_heatmap, "fig3_gene_frequency_heatmap", width = 12, height = 8)

# Gene prevalence line plot (proportion of cells carrying each gene)
n_cells_per_tp <- cells_df %>% group_by(TimePoint) %>% summarise(NCells = n(), .groups = "drop")

gene_cell_prop <- gene_counts_df %>%
  filter(Count > 0) %>%
  left_join(n_cells_per_tp, by = "TimePoint") %>%
  group_by(TimePoint, Gene) %>%
  summarise(
    NCellsWithGene = n_distinct(CellID),
    NCells         = first(NCells),
    Proportion     = NCellsWithGene / NCells * 100,
    .groups = "drop"
  )

p_gene_freq_line <- ggplot(gene_cell_prop, aes(x = TimePoint, y = Proportion, color = Gene)) +
  geom_line(linewidth = 0.8, alpha = 0.85) +
  geom_point(size = 1.5, alpha = 0.7) +
  scale_color_viridis_d(option = "H", name = "Gene") +
  labs(
    title = "Gene Prevalence Dynamics Across Evolutionary Time",
    subtitle = "Percentage of cells carrying each gene type",
    x = "Time Point (Iteration)",
    y = "Proportion of Cells Carrying Gene (%)"
  ) +
  theme_pub() +
  theme(
    plot.title  = element_text(hjust = 0.5, face = "bold"),
    plot.subtitle = element_text(hjust = 0.5),
    legend.position = "right",
    legend.text = element_text(size = 7)
  )

save_figure(p_gene_freq_line, "fig3b_gene_frequency_lines", width = 12, height = 7)

# =============================================================================
# FIGURE 4: Population Dynamics
# =============================================================================
cat("\n[INFO] Generating Figure 4: Population Dynamics...\n")

p_pop_dyn <- ggplot(diversity_metrics, aes(x = TimePoint)) +
  geom_area(aes(y = NAlive), fill = "#2196F3", alpha = 0.35) +
  geom_line(aes(y = NAlive), color = "#1565C0", linewidth = 1) +
  geom_point(aes(y = NAlive), color = "#1565C0", size = 2) +
  labs(
    title = "Population Size Dynamics Over Evolutionary Time",
    x = "Time Point (Iteration)",
    y = "Number of Living Cells"
  ) +
  theme_pub() +
  theme(plot.title = element_text(hjust = 0.5, face = "bold"))

save_figure(p_pop_dyn, "fig4_population_dynamics", width = 10, height = 6)

# =============================================================================
# FIGURE 5: Generation Distribution Over Time (Ridge Plot)
# =============================================================================
cat("\n[INFO] Generating Figure 5: Generation Distribution...\n")

p_gen_ridge <- ggplot(cells_df, aes(x = Generation, y = factor(TimePoint), fill = factor(TimePoint))) +
  geom_density_ridges(alpha = 0.6, scale = 1.5, linewidth = 0.3) +
  scale_fill_viridis_d(option = "D", name = "Time Point", begin = 0.2, end = 0.9) +
  scale_x_continuous(breaks = pretty) +
  labs(
    title = "Generation Distribution Across Evolutionary Time",
    x = "Cell Generation",
    y = "Time Point (Iteration)"
  ) +
  theme_pub() +
  theme(
    plot.title   = element_text(hjust = 0.5, face = "bold"),
    legend.position = "none"
  )

save_figure(p_gen_ridge, "fig5_generation_distribution", width = 10, height = 8)

# Generation trend
p_gen_trend <- ggplot(diversity_metrics, aes(x = TimePoint)) +
  geom_ribbon(aes(ymin = pmax(MeanGeneration - SDGeneration, 0),
                  ymax = MeanGeneration + SDGeneration),
              fill = "#E91E63", alpha = 0.15) +
  geom_line(aes(y = MeanGeneration), color = "#E91E63", linewidth = 1) +
  geom_point(aes(y = MeanGeneration), color = "#E91E63", size = 2) +
  geom_line(aes(y = MaxGeneration), color = "#9C27B0", linewidth = 0.8, linetype = "dashed") +
  geom_point(aes(y = MaxGeneration), color = "#9C27B0", size = 1.5, shape = 17) +
  labs(
    title = "Generation Progression Over Evolutionary Time",
    subtitle = "Solid line: mean generation; Dashed line: max generation",
    x = "Time Point (Iteration)",
    y = "Generation"
  ) +
  theme_pub() +
  theme(plot.title = element_text(hjust = 0.5, face = "bold"),
        plot.subtitle = element_text(hjust = 0.5))

save_figure(p_gen_trend, "fig5b_generation_trend", width = 10, height = 6)

# =============================================================================
# FIGURE 6: Genomic Diversity Over Time
# =============================================================================
cat("\n[INFO] Generating Figure 6: Genomic Diversity...\n")

p_jaccard <- ggplot(diversity_metrics %>% filter(!is.na(MeanJaccard)),
                    aes(x = TimePoint)) +
  geom_ribbon(aes(ymin = pmax(MeanJaccard - SDJaccard, 0),
                  ymax = pmin(MeanJaccard + SDJaccard, 1)),
              fill = "#FF9800", alpha = 0.2) +
  geom_line(aes(y = MeanJaccard), color = "#FF9800", linewidth = 1) +
  geom_point(aes(y = MeanJaccard), color = "#FF9800", size = 2) +
  scale_y_continuous(limits = c(0, 1)) +
  labs(
    title = "Mean Pairwise Genomic Jaccard Distance Over Time",
    subtitle = "Higher values indicate greater genomic divergence among cells",
    x = "Time Point (Iteration)",
    y = "Mean Jaccard Distance"
  ) +
  theme_pub() +
  theme(plot.title = element_text(hjust = 0.5, face = "bold"),
        plot.subtitle = element_text(hjust = 0.5))

save_figure(p_jaccard, "fig6_genomic_diversity_jaccard", width = 10, height = 6)

# Shannon diversity of gene composition per time point
shannon_diversity <- gene_counts_df %>%
  group_by(TimePoint, Gene) %>%
  summarise(TotalCount = sum(Count, na.rm = TRUE), .groups = "drop") %>%
  group_by(TimePoint) %>%
  mutate(
    Prop = TotalCount / sum(TotalCount),
    Prop = ifelse(Prop == 0, 1e-10, Prop)
  ) %>%
  summarise(
    ShannonH = -sum(Prop * log(Prop)),
    Richness = n_distinct(Gene),
    .groups = "drop"
  )

p_shannon <- ggplot(shannon_diversity, aes(x = TimePoint, y = ShannonH)) +
  geom_line(color = "#4CAF50", linewidth = 1) +
  geom_point(color = "#4CAF50", size = 2) +
  labs(
    title = "Shannon Diversity Index of Gene Composition Over Time",
    subtitle = "Measures evenness and richness of gene distribution in the population",
    x = "Time Point (Iteration)",
    y = "Shannon Diversity Index (H')"
  ) +
  theme_pub() +
  theme(plot.title = element_text(hjust = 0.5, face = "bold"),
        plot.subtitle = element_text(hjust = 0.5))

save_figure(p_shannon, "fig6b_shannon_diversity", width = 10, height = 6)

# Gene richness
p_richness <- ggplot(shannon_diversity, aes(x = TimePoint, y = Richness)) +
  geom_line(color = "#8BC34A", linewidth = 1) +
  geom_point(color = "#8BC34A", size = 2) +
  labs(
    title = "Gene Richness (Number of Distinct Gene Types) Over Time",
    x = "Time Point (Iteration)",
    y = "Gene Richness"
  ) +
  theme_pub() +
  theme(plot.title = element_text(hjust = 0.5, face = "bold"))

save_figure(p_richness, "fig6c_gene_richness", width = 10, height = 6)

# =============================================================================
# FIGURE 7: Plasmid Dynamics Over Time
# =============================================================================
cat("\n[INFO] Generating Figure 7: Plasmid Dynamics...\n")

p_plasmid <- ggplot(cells_df, aes(x = factor(TimePoint), fill = factor(PlasmidCount))) +
  geom_bar(position = "fill", alpha = 0.85, color = "white", linewidth = 0.2) +
  scale_fill_viridis_d(option = "E", name = "Plasmid\nCount") +
  scale_y_continuous(labels = percent_format()) +
  labs(
    title = "Proportion of Cells by Plasmid Count Over Time",
    x = "Time Point (Iteration)",
    y = "Proportion of Cells"
  ) +
  theme_pub() +
  theme(
    axis.text.x = element_text(angle = 45, hjust = 1),
    plot.title  = element_text(hjust = 0.5, face = "bold")
  )

save_figure(p_plasmid, "fig7_plasmid_dynamics", width = 10, height = 7)

# Mean plasmid count trend
p_plasmid_trend <- ggplot(diversity_metrics, aes(x = TimePoint, y = MeanPlasmidCount)) +
  geom_line(color = "#795548", linewidth = 1) +
  geom_point(color = "#795548", size = 2) +
  labs(
    title = "Mean Plasmid Count Per Cell Over Evolutionary Time",
    x = "Time Point (Iteration)",
    y = "Mean Plasmid Count"
  ) +
  theme_pub() +
  theme(plot.title = element_text(hjust = 0.5, face = "bold"))

save_figure(p_plasmid_trend, "fig7b_plasmid_trend", width = 10, height = 6)

# =============================================================================
# FIGURE 8: Genome Composition Stacked Bar
# =============================================================================
cat("\n[INFO] Generating Figure 8: Genome Composition...\n")

gene_composition <- gene_counts_df %>%
  group_by(TimePoint, Gene) %>%
  summarise(TotalCount = sum(Count, na.rm = TRUE), .groups = "drop") %>%
  group_by(TimePoint) %>%
  mutate(Proportion = TotalCount / sum(TotalCount) * 100) %>%
  ungroup()

p_composition <- ggplot(gene_composition, aes(x = factor(TimePoint), y = Proportion, fill = Gene)) +
  geom_col(position = "stack", width = 0.7, color = "white", linewidth = 0.1) +
  scale_fill_viridis_d(option = "H", name = "Gene") +
  labs(
    title = "Genome Composition Across Evolutionary Time",
    subtitle = "Relative proportion of each gene in the total gene pool",
    x = "Time Point (Iteration)",
    y = "Proportion of Gene Pool (%)"
  ) +
  theme_pub() +
  theme(
    axis.text.x = element_text(angle = 45, hjust = 1),
    plot.title  = element_text(hjust = 0.5, face = "bold"),
    plot.subtitle = element_text(hjust = 0.5),
    legend.text = element_text(size = 7),
    legend.position = "right"
  )

save_figure(p_composition, "fig8_genome_composition", width = 12, height = 8)

# =============================================================================
# FIGURE 9: Total Gene Count Per Cell Distribution
# =============================================================================
cat("\n[INFO] Generating Figure 9: Gene Count Distribution...\n")

p_gene_count_dist <- ggplot(cells_df, aes(x = NTotalGenes, fill = factor(TimePoint))) +
  geom_density(alpha = 0.4, linewidth = 0.5) +
  scale_fill_viridis_d(option = "D", name = "Time Point", begin = 0.2, end = 0.9) +
  labs(
    title = "Distribution of Total Gene Count Per Cell Across Time",
    x = "Total Number of Genes (Genome + Plasmids)",
    y = "Density"
  ) +
  theme_pub() +
  theme(plot.title = element_text(hjust = 0.5, face = "bold"))

save_figure(p_gene_count_dist, "fig9_gene_count_distribution", width = 10, height = 6)

# =============================================================================
# FIGURE 10: Environmental Selection Pressure Indicators
# =============================================================================
cat("\n[INFO] Generating Figure 10: Environmental Selection Indicators...\n")

# pH distribution over time
p_ph <- ggplot(cells_df, aes(x = factor(TimePoint), y = PH)) +
  geom_violin(aes(fill = factor(TimePoint)), alpha = 0.4, show.legend = FALSE, linewidth = 0.3) +
  geom_boxplot(width = 0.15, outlier.size = 0.4, linewidth = 0.3, fill = "white") +
  scale_fill_viridis_d(option = "D", begin = 0.2, end = 0.9) +
  labs(
    title = "Intracellular pH Distribution Over Evolutionary Time",
    subtitle = "Indicator of environmental selection pressure on cellular homeostasis",
    x = "Time Point (Iteration)",
    y = "Intracellular pH"
  ) +
  theme_pub() +
  theme(
    axis.text.x = element_text(angle = 45, hjust = 1),
    plot.title  = element_text(hjust = 0.5, face = "bold"),
    plot.subtitle = element_text(hjust = 0.5)
  )

save_figure(p_ph, "fig10_ph_distribution", width = 12, height = 7)

# Cell wall acquisition over time
cellwall_prop <- cells_df %>%
  group_by(TimePoint) %>%
  summarise(
    PropWithWall = mean(HasCellWall, na.rm = TRUE) * 100,
    .groups = "drop"
  )

p_cellwall <- ggplot(cellwall_prop, aes(x = TimePoint, y = PropWithWall)) +
  geom_area(fill = "#00BCD4", alpha = 0.3) +
  geom_line(color = "#00838F", linewidth = 1) +
  geom_point(color = "#00838F", size = 2) +
  scale_y_continuous(limits = c(0, 100), labels = function(x) paste0(x, "%")) +
  labs(
    title = "Proportion of Cells With Cell Wall Over Time",
    subtitle = "Cell wall acquisition as an adaptive response to environmental stress",
    x = "Time Point (Iteration)",
    y = "Proportion With Cell Wall"
  ) +
  theme_pub() +
  theme(plot.title = element_text(hjust = 0.5, face = "bold"),
        plot.subtitle = element_text(hjust = 0.5))

save_figure(p_cellwall, "fig10b_cell_wall_adaptation", width = 10, height = 6)

# =============================================================================
# FIGURE 11: Gene Gain and Loss Dynamics
# =============================================================================
cat("\n[INFO] Generating Figure 11: Gene Gain/Loss Dynamics...\n")

# Identify ancestor genes (generation 0)
ancestor_genes <- cells_df %>%
  filter(Generation == 0) %>%
  pull(AllGenesStr) %>%
  strsplit(",") %>%
  unlist() %>%
  unique()
ancestor_genes <- ancestor_genes[ancestor_genes != ""]

cat(sprintf("[INFO] Ancestor genes (%d): %s\n", length(ancestor_genes),
            paste(ancestor_genes, collapse = ", ")))

# For each time point, identify gained and lost genes relative to ancestor
gene_gain_loss <- cells_df %>%
  group_by(TimePoint) %>%
  summarise(
    AllGenesList = list(unique(unlist(strsplit(AllGenesStr, ",")))),
    .groups = "drop"
  ) %>%
  rowwise() %>%
  mutate(
    CurrentGenes = list(AllGenesList[[1]][AllGenesList[[1]] != ""]),
    NGained  = length(setdiff(CurrentGenes[[1]], ancestor_genes)),
    NLost    = length(setdiff(ancestor_genes, CurrentGenes[[1]])),
    NShared  = length(intersect(CurrentGenes[[1]], ancestor_genes)),
    .groups  = "drop"
  ) %>%
  ungroup() %>%
  select(TimePoint, NGained, NLost, NShared)

p_gain_loss <- ggplot(gene_gain_loss, aes(x = TimePoint)) +
  geom_line(aes(y = NGained, color = "Gained"), linewidth = 1) +
  geom_point(aes(y = NGained, color = "Gained"), size = 2) +
  geom_line(aes(y = NLost, color = "Lost"), linewidth = 1) +
  geom_point(aes(y = NLost, color = "Lost"), size = 2) +
  geom_line(aes(y = NShared, color = "Shared"), linewidth = 1, linetype = "dashed") +
  geom_point(aes(y = NShared, color = "Shared"), size = 2, shape = 17) +
  scale_color_manual(
    name = "Category",
    values = c("Gained" = "#4CAF50", "Lost" = "#F44336", "Shared" = "#9E9E9E"),
    labels = c("Gained", "Lost", "Shared with Ancestor")
  ) +
  labs(
    title = "Gene Gain and Loss Dynamics Relative to Ancestor",
    subtitle = "Tracking genomic innovation and gene loss under selection pressure",
    x = "Time Point (Iteration)",
    y = "Number of Distinct Genes"
  ) +
  theme_pub() +
  theme(
    plot.title    = element_text(hjust = 0.5, face = "bold"),
    plot.subtitle = element_text(hjust = 0.5),
    legend.position = "top"
  )

save_figure(p_gain_loss, "fig11_gene_gain_loss", width = 10, height = 7)

# =============================================================================
# FIGURE 12: Comprehensive Multi-Panel Summary
# =============================================================================
cat("\n[INFO] Generating Figure 12: Multi-Panel Summary...\n")

p_panel_a <- ggplot(diversity_metrics, aes(x = TimePoint, y = NAlive)) +
  geom_line(color = "#1565C0", linewidth = 0.8) +
  geom_point(color = "#1565C0", size = 1.5) +
  labs(x = "Time Point", y = "Living Cells", title = "A) Population Size") +
  theme_pub(base_size = 9)

p_panel_b <- ggplot(diversity_metrics, aes(x = TimePoint, y = MeanGenomeSize)) +
  geom_ribbon(aes(ymin = MeanGenomeSize - SDGenomeSize,
                  ymax = MeanGenomeSize + SDGenomeSize),
              fill = "steelblue", alpha = 0.2) +
  geom_line(color = "steelblue", linewidth = 0.8) +
  labs(x = "Time Point", y = "Mean Genome Size (bp)", title = "B) Genome Size") +
  theme_pub(base_size = 9)

p_panel_c <- ggplot(diversity_metrics %>% filter(!is.na(MeanJaccard)),
                    aes(x = TimePoint, y = MeanJaccard)) +
  geom_ribbon(aes(ymin = pmax(MeanJaccard - SDJaccard, 0),
                  ymax = pmin(MeanJaccard + SDJaccard, 1)),
              fill = "#FF9800", alpha = 0.2) +
  geom_line(color = "#FF9800", linewidth = 0.8) +
  labs(x = "Time Point", y = "Mean Jaccard Distance", title = "C) Genomic Divergence") +
  theme_pub(base_size = 9)

p_panel_d <- ggplot(diversity_metrics, aes(x = TimePoint, y = MeanGeneration)) +
  geom_ribbon(aes(ymin = pmax(MeanGeneration - SDGeneration, 0),
                  ymax = MeanGeneration + SDGeneration),
              fill = "#E91E63", alpha = 0.15) +
  geom_line(color = "#E91E63", linewidth = 0.8) +
  labs(x = "Time Point", y = "Mean Generation", title = "D) Generation Progression") +
  theme_pub(base_size = 9)

p_panel_e <- ggplot(shannon_diversity, aes(x = TimePoint, y = ShannonH)) +
  geom_line(color = "#4CAF50", linewidth = 0.8) +
  geom_point(color = "#4CAF50", size = 1.5) +
  labs(x = "Time Point", y = "Shannon H'", title = "E) Gene Diversity") +
  theme_pub(base_size = 9)

p_panel_f <- ggplot(diversity_metrics, aes(x = TimePoint, y = MeanPlasmidCount)) +
  geom_line(color = "#795548", linewidth = 0.8) +
  geom_point(color = "#795548", size = 1.5) +
  labs(x = "Time Point", y = "Mean Plasmid Count", title = "F) Plasmid Load") +
  theme_pub(base_size = 9)

p_summary <- (p_panel_a + p_panel_b + p_panel_c) /
             (p_panel_d + p_panel_e + p_panel_f) +
  plot_annotation(
    title = "Comprehensive Evolutionary Dynamics Summary",
    theme = theme(plot.title = element_text(hjust = 0.5, face = "bold", size = 14))
  )

save_figure(p_summary, "fig12_multi_panel_summary", width = 14, height = 10)

# =============================================================================
# FIGURE 13: Spatial Distribution of Cells
# =============================================================================
cat("\n[INFO] Generating Figure 13: Spatial Distribution...\n")

p_spatial <- ggplot(cells_df, aes(x = PosX, y = PosY, color = Generation)) +
  geom_point(size = 1.5, alpha = 0.6) +
  facet_wrap(~ factor(TimePoint), ncol = 4) +
  scale_color_viridis_c(option = "C", name = "Generation") +
  labs(
    title = "Spatial Distribution of Cells (X-Y Projection)",
    x = "X Position",
    y = "Y Position"
  ) +
  theme_pub() +
  theme(
    plot.title = element_text(hjust = 0.5, face = "bold"),
    strip.text = element_text(size = 8)
  )

save_figure(p_spatial, "fig13_spatial_distribution", width = 14, height = 10)

# =============================================================================
# FIGURE 14: Metabolic Profile Evolution
# =============================================================================
cat("\n[INFO] Generating Figure 14: Metabolic Profile...\n")

metabolic_molecules <- c("Glucose", "CarbonSource", "Nucleotide", "ATP",
                         "CarbonDioxide", "Oxygen", "Pyruvate", "Phosphate")

metabolic_df <- molecules_df %>%
  filter(Molecule %in% metabolic_molecules)

if (nrow(metabolic_df) > 0) {
  p_metabolic <- ggplot(metabolic_df, aes(x = factor(TimePoint), y = log10(Amount + 1),
                                          fill = Molecule)) +
    geom_boxplot(outlier.size = 0.3, linewidth = 0.3, alpha = 0.7) +
    facet_wrap(~ Molecule, scales = "free_y", ncol = 4) +
    scale_fill_viridis_d(option = "H", guide = "none") +
    labs(
      title = "Metabolic Profile Evolution Across Time Points",
      subtitle = "Log10-transformed intracellular molecule amounts",
      x = "Time Point (Iteration)",
      y = expression(log[10](Amount + 1))
    ) +
    theme_pub(base_size = 9) +
    theme(
      axis.text.x = element_text(angle = 45, hjust = 1, size = 7),
      plot.title  = element_text(hjust = 0.5, face = "bold"),
      plot.subtitle = element_text(hjust = 0.5),
      strip.text  = element_text(size = 8, face = "italic")
    )

  save_figure(p_metabolic, "fig14_metabolic_profile", width = 14, height = 10)
} else {
  cat("[WARN] No metabolic molecule data available. Skipping Figure 14.\n")
}

# =============================================================================
# FIGURE 15: Evolutionary Rate Analysis
# =============================================================================
cat("\n[INFO] Generating Figure 15: Evolutionary Rate...\n")

evo_rate <- diversity_metrics %>%
  arrange(TimePoint) %>%
  mutate(
    GenomeSizeRate = c(NA, diff(MeanGenomeSize)),
    GenerationRate = c(NA, diff(MeanGeneration))
  ) %>%
  filter(!is.na(GenomeSizeRate))

p_evo_rate <- ggplot(evo_rate, aes(x = TimePoint)) +
  geom_col(aes(y = GenomeSizeRate, fill = GenomeSizeRate > 0), alpha = 0.8) +
  scale_fill_manual(
    name = "Direction",
    values = c("TRUE" = "#4CAF50", "FALSE" = "#F44336"),
    labels = c("Contraction", "Expansion")
  ) +
  labs(
    title = "Genome Size Evolutionary Rate Over Time",
    subtitle = "Rate of change in mean genome size between consecutive time points",
    x = "Time Point (Iteration)",
    y = expression(Delta * "Mean Genome Size (bp)")
  ) +
  theme_pub() +
  theme(
    plot.title    = element_text(hjust = 0.5, face = "bold"),
    plot.subtitle = element_text(hjust = 0.5),
    legend.position = "top"
  )

save_figure(p_evo_rate, "fig15_evolutionary_rate", width = 10, height = 6)

# =============================================================================
# FIGURE 16: Genome-Generation Relationship
# =============================================================================
cat("\n[INFO] Generating Figure 16: Genome-Generation Relationship...\n")

p_gen_genome <- ggplot(cells_df, aes(x = Generation, y = GenomeSize)) +
  geom_point(aes(color = factor(TimePoint)), alpha = 0.5, size = 1.5) +
  geom_smooth(method = "loess", se = TRUE, color = "black", linewidth = 0.8, alpha = 0.2) +
  scale_color_viridis_d(option = "D", name = "Time Point", begin = 0.2, end = 0.9) +
  labs(
    title = "Relationship Between Cell Generation and Genome Size",
    subtitle = "With LOESS regression trend line",
    x = "Cell Generation",
    y = "Genome Size (bp)"
  ) +
  theme_pub() +
  theme(
    plot.title    = element_text(hjust = 0.5, face = "bold"),
    plot.subtitle = element_text(hjust = 0.5)
  )

save_figure(p_gen_genome, "fig16_generation_vs_genome_size", width = 10, height = 7)

# =============================================================================
# FIGURE 17: Gene Co-occurrence Matrix
# =============================================================================
cat("\n[INFO] Generating Figure 17: Gene Co-occurrence...\n")

gene_sets_list <- strsplit(cells_df$AllGenesStr, ",")
all_gene_types <- sort(unique(unlist(gene_sets_list)))
all_gene_types <- all_gene_types[all_gene_types != ""]

n_genes <- length(all_gene_types)

if (n_genes > 1) {
  # Build co-occurrence matrix
  cooccurrence <- matrix(0, nrow = n_genes, ncol = n_genes)
  rownames(cooccurrence) <- all_gene_types
  colnames(cooccurrence) <- all_gene_types

  for (gs in gene_sets_list) {
    gs <- unique(gs[gs != ""])
    if (length(gs) > 0) {
      for (g1 in gs) {
        for (g2 in gs) {
          cooccurrence[g1, g2] <- cooccurrence[g1, g2] + 1
        }
      }
    }
  }

  # Convert to co-occurrence proportion
  n_total <- nrow(cells_df)
  cooc_prop <- cooccurrence / n_total

  cooc_melt <- melt(cooc_prop)
  colnames(cooc_melt) <- c("Gene1", "Gene2", "Cooccurrence")

  p_cooc <- ggplot(cooc_melt, aes(x = Gene1, y = Gene2, fill = Cooccurrence)) +
    geom_tile(color = "white", linewidth = 0.3) +
    scale_fill_viridis_c(option = "A", name = "Co-occurrence\nProportion") +
    labs(
      title = "Gene Co-occurrence Matrix",
      subtitle = "Proportion of cells in which two genes co-occur",
      x = "",
      y = ""
    ) +
    theme_pub() +
    theme(
      axis.text.x  = element_text(angle = 45, hjust = 1, size = 8),
      axis.text.y  = element_text(size = 8),
      plot.title   = element_text(hjust = 0.5, face = "bold"),
      plot.subtitle = element_text(hjust = 0.5)
    )

  save_figure(p_cooc, "fig17_gene_cooccurrence", width = 10, height = 9)
} else {
  cat("[WARN] Insufficient gene types for co-occurrence analysis. Skipping Figure 17.\n")
}

# =============================================================================
# FIGURE 18: Lineage Survival Analysis
# =============================================================================
cat("\n[INFO] Generating Figure 18: Lineage Survival...\n")

survival_by_gen <- cells_df %>%
  group_by(Generation) %>%
  summarise(
    NTotal = n(),
    NAlive = sum(IsAlive),
    SurvivalRate = NAlive / NTotal * 100,
    .groups = "drop"
  )

p_survival <- ggplot(survival_by_gen, aes(x = factor(Generation), y = SurvivalRate)) +
  geom_col(fill = "#3F51B5", alpha = 0.8, width = 0.7) +
  geom_text(aes(label = sprintf("%.0f%%", SurvivalRate)),
            vjust = -0.3, size = 3, color = "#3F51B5") +
  labs(
    title = "Cell Survival Rate by Generation",
    subtitle = "Proportion of living cells within each generation cohort",
    x = "Cell Generation",
    y = "Survival Rate (%)"
  ) +
  theme_pub() +
  theme(plot.title = element_text(hjust = 0.5, face = "bold"),
        plot.subtitle = element_text(hjust = 0.5))

save_figure(p_survival, "fig18_lineage_survival", width = 10, height = 6)

# =============================================================================
# FIGURE 19: Metabolic Strategy Gene Dynamics
# =============================================================================
cat("\n[INFO] Generating Figure 19: Metabolic Strategy Dynamics...\n")

metabolic_genes <- c("AerobicEnergyMetabolismATP", "AnaerobicEnergyMetabolismATP",
                     "GlucoseConversionEnzyme", "DegradeMacromolecule",
                     "PolyphosphateKinase", "AminoMixThiolEnzyme")

metabolic_gene_prop <- gene_counts_df %>%
  filter(Gene %in% metabolic_genes & Count > 0) %>%
  left_join(n_cells_per_tp, by = "TimePoint") %>%
  group_by(TimePoint, Gene) %>%
  summarise(
    NCellsWithGene = n_distinct(CellID),
    NCells         = first(NCells),
    Proportion     = NCellsWithGene / NCells * 100,
    .groups = "drop"
  )

p_metabolic_strategy <- ggplot(metabolic_gene_prop,
                               aes(x = TimePoint, y = Proportion, color = Gene)) +
  geom_line(linewidth = 0.9, alpha = 0.85) +
  geom_point(size = 2, alpha = 0.7) +
  scale_color_manual(
    name = "Metabolic Gene",
    values = c(
      "AerobicEnergyMetabolismATP"   = "#E53935",
      "AnaerobicEnergyMetabolismATP" = "#1E88E5",
      "GlucoseConversionEnzyme"      = "#43A047",
      "DegradeMacromolecule"         = "#FB8C00",
      "PolyphosphateKinase"          = "#8E24AA",
      "AminoMixThiolEnzyme"          = "#00ACC1"
    )
  ) +
  labs(
    title = "Metabolic Strategy Gene Prevalence Over Evolutionary Time",
    subtitle = "Tracking aerobic vs anaerobic metabolic pathway selection",
    x = "Time Point (Iteration)",
    y = "Proportion of Cells Carrying Gene (%)"
  ) +
  theme_pub() +
  theme(
    plot.title    = element_text(hjust = 0.5, face = "bold"),
    plot.subtitle = element_text(hjust = 0.5),
    legend.position = "right"
  )

save_figure(p_metabolic_strategy, "fig19_metabolic_strategy", width = 11, height = 7)

# =============================================================================
# FIGURE 20: Genome Streamlining Index
# =============================================================================
cat("\n[INFO] Generating Figure 20: Genome Streamlining...\n")

cells_df$StreamliningIndex <- cells_df$NTotalGenes / cells_df$GenomeSize

p_streamline <- ggplot(cells_df, aes(x = factor(TimePoint), y = StreamliningIndex)) +
  geom_violin(aes(fill = factor(TimePoint)), alpha = 0.4, show.legend = FALSE, linewidth = 0.3) +
  geom_boxplot(width = 0.15, outlier.size = 0.4, linewidth = 0.3, fill = "white") +
  scale_fill_viridis_d(option = "D", begin = 0.2, end = 0.9) +
  labs(
    title = "Genome Streamlining Index Across Evolutionary Time",
    subtitle = "Ratio of unique gene count to genome size (lower = more streamlined)",
    x = "Time Point (Iteration)",
    y = "Streamlining Index (Unique Genes / Genome Size)"
  ) +
  theme_pub() +
  theme(
    axis.text.x = element_text(angle = 45, hjust = 1),
    plot.title  = element_text(hjust = 0.5, face = "bold"),
    plot.subtitle = element_text(hjust = 0.5)
  )

save_figure(p_streamline, "fig20_genome_streamlining", width = 12, height = 7)

# =============================================================================
# Final Summary
# =============================================================================
cat("\n")
cat("=============================================================================\n")
cat("  Microbial Evolution Visualization Complete\n")
cat("=============================================================================\n")
cat(sprintf("  Output directory: %s\n", OUTPUT_DIR))
cat("  Figure list:\n")
cat("    fig1   - Phylogenetic Tree (Rectangular Layout)\n")
cat("    fig1b  - Phylogenetic Tree (Circular Layout)\n")
cat("    fig1c  - Phylogenetic Tree Colored by Genome Size\n")
cat("    fig2   - Genome Size Distribution (Violin + Boxplot)\n")
cat("    fig2b  - Genome Size Trend Line\n")
cat("    fig3   - Gene Copy Number Heatmap\n")
cat("    fig3b  - Gene Prevalence Dynamics (Line Plot)\n")
cat("    fig4   - Population Size Dynamics\n")
cat("    fig5   - Generation Distribution (Ridge Plot)\n")
cat("    fig5b  - Generation Trend\n")
cat("    fig6   - Genomic Jaccard Distance Over Time\n")
cat("    fig6b  - Shannon Diversity Index\n")
cat("    fig6c  - Gene Richness\n")
cat("    fig7   - Plasmid Count Proportion\n")
cat("    fig7b  - Plasmid Count Trend\n")
cat("    fig8   - Genome Composition (Stacked Bar)\n")
cat("    fig9   - Gene Count Distribution (Density)\n")
cat("    fig10  - Intracellular pH Distribution\n")
cat("    fig10b - Cell Wall Adaptation\n")
cat("    fig11  - Gene Gain and Loss Dynamics\n")
cat("    fig12  - Multi-Panel Summary (6 panels)\n")
cat("    fig13  - Spatial Distribution\n")
cat("    fig14  - Metabolic Profile Evolution\n")
cat("    fig15  - Evolutionary Rate Analysis\n")
cat("    fig16  - Generation vs Genome Size\n")
cat("    fig17  - Gene Co-occurrence Matrix\n")
cat("    fig18  - Lineage Survival Rate\n")
cat("    fig19  - Metabolic Strategy Dynamics\n")
cat("    fig20  - Genome Streamlining Index\n")
cat("=============================================================================\n")
