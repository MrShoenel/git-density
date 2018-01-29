library(RMySQL)
library(rjson)
source("./Settings.R")
source("./Functions/queries.R")


d23 <- getData(function() getQuery_allContributions(smtType = 23))
summary(d23)
