getQuery_allContributions <- function(smtType = 0, includePureAdds = TRUE, minHours = 0.1, maxHours = Inf, maxDiff = 120, firstCommitAdd = 120) {
	# Obtains a query for all aggregated contributions.
	#
	# Args:
	#		smtType		: The integer-value of one of the Util.Similarity.SimilarityMeasurementType
	#		minHours	: A positive float representing the minimum hours to consider for contributions
	#		maxHours	:	A positive float representing the maximum hours to consider
	#		maxDiff		:	The amount of minutes in between commits to start a new session.
	#		firstCommitAdd	:	The amount of minutes to add for initial (session or global) commits. Currently disabled/ignored.
	#		includePureAdds	:	Whether or not to include changes that were pure adds.
	#
	#	Returns:
	#		The query, ready to be used in a script to obtain data.
	#

	temp <- iconv(
		paste(readLines(
			'./Queries/all-contribs-for-1smt-mixed.mysql', n = -1, warn = FALSE, encoding = 'UTF-8'),
			collapse = '\n'),
		from = 'UTF-8', to = 'ASCII', sub = '')
	temp <- gsub('__smt__', smtType, temp, ignore.case = TRUE)
	temp <- gsub('__min_hours__', minHours, temp, ignore.case = TRUE)
	temp <- gsub('__max_hours__', ifelse(maxHours == Inf, 9999999, maxHours), temp, ignore.case = TRUE)
	temp <- gsub('__max_diff__', maxDiff, temp, ignore.case = TRUE)
	temp <- gsub('__first_add__', firstCommitAdd, temp, ignore.case = TRUE)

	if (includePureAdds) {
		temp <- gsub('__en_add_start__', '', temp)
		temp <- gsub('__en_add_end__', '', temp)
	} else {
		temp <- gsub('__en_add_start__', '/*', temp)
		temp <- gsub('__en_add_end__', '*/', temp)
	}

	return(temp)
}


getQuery_allHoursTypesNoFirstCommit <- function() {
	# Returns a query that can be used to obtain all distinct Hours-Types (maximum session lengths).
	# This query ignores FirstCommitAddMinutes and returns only the MaxCommitDiffMinutes.
	#
	#	Returns:
	#		The (MySQL-compatible) query, ready to be used in a statement.
	return ("SELECT DISTINCT(MaxCommitDiffMinutes) FROM hours_type_entity WHERE MaxCommitDiffMinutes IN (120,180,240,360,480,720,1080,1440)
		ORDER BY MaxCommitDiffMinutes;")
}


getQuery_allUsedSmtTypes <- function() {
	# Returns a query that can be used to obtain all distinct used similarity measurement types.
	# C.f. (Int32)Util.Similarity.SimilarityMeasurementType
	#
	#	Returns:
	#		The (MySQL-compatible) query, ready to be used in a statement.
	return ("SELECT SimilarityMeasurementType FROM tree_entry_contribution_entity GROUP BY SimilarityMeasurementType ORDER BY SimilarityMeasurementType;")
}


getData <- function(queryFn) {
	# Executes a query and returns the data-frame, while suppressing errors.
	#
	#	Args:
	#		queryFn:	A parameterless function that, when executed, returns the query as string
	#
	#	Returns:
	#		The data frame.
	#

	dbConfig <- settings$dbConfig

	mydb <- dbConnect(MySQL(), user = dbConfig$user, password = dbConfig$password,
		dbname = dbConfig$dbname, host = dbConfig$host)

	oldw <- getOption("warn")
	options(warn = -1)

	reader <- dbSendQuery(mydb, queryFn())
	data <- fetch(reader, n = Inf)

	dbDisconnect(mydb)

	options(warn = oldw)

	return(data)
}