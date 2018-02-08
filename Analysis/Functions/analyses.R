#library(doSNOW) # parallel backend
#library(foreach) # for parallel loops!

# Initi parallel backend
#cl <- makeCluster(8)
#registerDoSNOW(cl)


#library(RWeka)

getAnalysis_hoursSizeCorrelations <- function() {

	hours_types <- getData(function() getQuery_allHoursTypesNoFirstCommit())
	smt_types <- getData(function() getQuery_allUsedSmtTypes())
	resultsNumRows <- nrow(hours_types) * nrow(smt_types)

	corrColumns <- c(
		"hour_NumTotal",
		"hour_NumNoCommentsTotal",
		"hour_NumPostCloneTotal",
		"hour_NumPostCloneNoCommentsTotal",
		"hour_NumClonedBlockLinesTotal",
		"hour_NumClonedBlockLinesNoCommentsTotal"
	)

	corrToCompute <- c("spearman", "kendall", "pearson")

	results <- data.frame(
		"dataset_id" = character(resultsNumRows),
		stringsAsFactors = FALSE
	)

	for (corCol in corrColumns) {
		for (corr in corrToCompute) {
			colName <- paste(corr, "corr", corCol, sep = "_")
			# Add column to result:
			results[colName] <- vector(mode = "numeric", length = resultsNumRows)
		}
	}

	resultsColnames <- colnames(results)

	#foo <- foreach(htIdx = rownames(hours_types)) %dopar% {
		#htIdxInt <- as.integer(htIdx)
		#ht <- hours_types$MaxCommitDiffMinutes[htIdxInt]

		#for (smt in smt_types$SimilarityMeasurementType) {
			#i <- htIdxInt * nrow(hours_types) + match(c(smt), smt_types$SimilarityMeasurementType)

			#dataset_id <- paste("ht-", ht, "_smt-", smt, sep = "")
			#print(paste("Getting dataset ID", dataset_id))
			#df <- getData(function() getQuery_allContributions(
				#smtType = smt, maxDiff = ht, minHours = .05))

			#results$dataset_id[i] <- dataset_id
			#for (corCol in corrColumns) {
				#for (corr in corrToCompute) {
					#colName <- paste(corr, "corr", corCol, sep = "_")
					#corrTypeName <- gsub("hour_", "", corCol)
					#results[i, colName] <- cor(
						#df["Hours"], df[corrTypeName], method = corr)
				#}
			#}
		#}
	#}

	#print(foo)

	i <- 1
	for (ht in hours_types$MaxCommitDiffMinutes) {
		for (smt in smt_types$SimilarityMeasurementType) {
			dataset_id <- paste("ht-", ht, "_smt-", smt, sep = "")
			print(paste("Getting dataset ID", dataset_id))
			df <- getData(function() getQuery_allContributions(smtType = smt, maxDiff = ht))

			results$dataset_id[i] <- dataset_id
			for (corCol in corrColumns) {
				for (corr in corrToCompute) {
					colName <- paste(corr, "corr", corCol, sep = "_")
					corrTypeName <- gsub("hour_", "", corCol)
					results[i, colName] <- cor(df["Hours"], df[corrTypeName], method = corr)
				}
			}

			write.csv(results, paste(dataset_id, "csv", sep = "."))

			i <- i + 1
		}
	}

	return(results)
}