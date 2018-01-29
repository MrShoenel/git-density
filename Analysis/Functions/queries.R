getQuery_allContributions <- function(smtType = 0, includePureAddsDels = FALSE, minHours = 0.1, maxHours = Inf) {
	# Obtains a query for all aggregated contributions.
	#
	# Args:
	#		smtType		: The integer-value of one of the Util.Similarity.SimilarityMeasurementType
	#		minHours	: A positive float representing the minimum hours to consider for contributions
	#		maxHours	:	A positive float representing the maximum hours to consider
	#
	#	Returns:
	#		The query, ready to be used in a script to obtain data.
	#

	temp <- iconv(
		paste(readLines(
			'./Queries/all-contribs-for-1smt-mixed.sql', n = -1, warn = FALSE, encoding = 'UTF-8'),
			collapse = '\n'),
		from = 'UTF-8', to = 'ASCII', sub = '')
	temp <- gsub('__smt__', smtType, temp, ignore.case = TRUE)
	temp <- gsub('__min_hours__', minHours, temp, ignore.case = TRUE)
	temp <- gsub('__max_hours__', ifelse(maxHours == Inf, 9999999, maxHours), temp, ignore.case = TRUE)
	if (includePureAddsDels) {
		temp <- gsub('__en_ad_start__', '', temp)
		temp <- gsub('__en_ad_end__', '', temp)
	} else {
		temp <- gsub('__en_ad_start__', '/*', temp)
		temp <- gsub('__en_ad_end__', '*/', temp)
	}

	return(temp)
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