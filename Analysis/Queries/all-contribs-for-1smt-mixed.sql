/*
 * Very similar to 'all-contribs-for-1smt.sql', but is able to merge amount of lines across
 * tree-statuses and similarity measurement types. The practical use is to aggregate commits'
 * added/deleted files as well as the changed/moved files and, at the same time, apply a specific
 * similarity measurement type to the changed/moved files.
 *
 * Parameters:
 * __smt__				: The type of Similarity Measure, see Util.Similarity.SimilarityMeasurementType
 * __min_hours__	:	The minimum amount of hours to consider (commits with less time will be cut off)
 * __max_hours__	: The maximum amount of hours to consider (note that this usually does not exceed 2.02)
 * en_ad_start		: If pure adds/deletes should be included, this will be subsituted with nothing; if not,
 *									then it will be substituted with the start of a multi-line comment.
 * en_ad_end			: Like __en_ad_start__, this will be substituted with nothing; if not, then it will be
 *									substituted with the end of a multi-line comment
 */

SELECT * FROM
(
SELECT
  _project.INTERNAL_ID as ProjectId,
  _repo.ID AS RepoId,
  _commit.ID AS CommitId,
  _commit.CommitDate,
  _contrib.Developer_id AS DeveloperId,
  _project.LANGUAGE+0 as Lang, /* Cast to numeric value */
  _hours.hours AS Hours,
  
	/* Ignore these for now */
	/*
  GROUP_CONCAT(distinct(_contrib.SimilarityMeasurementType) SEPARATOR ', ') as SimMeasureTypes,
  GROUP_CONCAT(_tree.ID SEPARATOR ', ') as TreeIds,
  GROUP_CONCAT(distinct(_tree.Status) SEPARATOR ', ') as TreeStatuses,
  GROUP_CONCAT(_metrics.ID SEPARATOR ', ') as MetricsIds,*/
	COUNT(distinct(_tree.Status)) as NumTreeStatuses,
	SUM(IF(_tree.Status=1, 1, 0)) AS TreeStat_1,
	SUM(IF(_tree.Status=2, 1, 0)) AS TreeStat_2,
	SUM(IF(_tree.Status=3, 1, 0)) AS TreeStat_3,
	SUM(IF(_tree.Status=4, 1, 0)) AS TreeStat_4,
  COUNT(_metrics.ID) as AggregatedMetrics,
	
	(SUM(_metrics.NumAdded) + SUM(_metrics.NumDeleted)) / IF(_hours.hours=0, 0.0001, _hours.hours) AS Productivity,
	(SUM(_metrics.NumAddedNoComments) + SUM(_metrics.NumDeletedNoComments)) / IF(_hours.hours=0, 0.0001, _hours.hours) AS ProductivityNoComments,
	(SUM(_metrics.NumAddedPostCloneDetection) + SUM(_metrics.NumDeletedPostCloneDetection)) / IF(_hours.hours=0, 0.0001, _hours.hours) AS ProductivityPostClone,
	(SUM(_metrics.NumAddedPostCloneDetectionNoComments) + SUM(_metrics.NumDeletedPostCloneDetectionNoComments)) / IF(_hours.hours=0, 0.0001, _hours.hours) AS ProductivityPostCloneNoComments,
	(SUM(_metrics.NumAddedClonedBlockLines) + SUM(_metrics.NumDeletedClonedBlockLines)) / IF(_hours.hours=0, 0.0001, _hours.hours) AS ProductivityClonedBlockLines,
	(SUM(_metrics.NumAddedClonedBlockLinesNoComments) + SUM(_metrics.NumDeletedClonedBlockLinesNoComments)) / IF(_hours.hours=0, 0.0001, _hours.hours) AS ProductivityClonedBlockLinesNoComments,

  SUM(_metrics.NumAdded) as NumAdded,
  SUM(_metrics.NumDeleted) as NumDeleted,
  SUM(_metrics.NumAdded) + SUM(_metrics.NumDeleted) as NumTotal,
  SUM(_metrics.NumAddedNoComments) as NumAddedNoComments,
  SUM(_metrics.NumDeletedNoComments) as NumDeletedNoComments,
  SUM(_metrics.NumAddedNoComments) + SUM(_metrics.NumDeletedNoComments) as NumNoCommentsTotal,
  
  SUM(_metrics.NumAddedPostCloneDetection) as NumAddedPostCloneDetection,
  SUM(_metrics.NumDeletedPostCloneDetection) as NumDeletedPostCloneDetection,
  SUM(_metrics.NumAddedPostCloneDetection) + SUM(_metrics.NumDeletedPostCloneDetection) as NumPostCloneTotal,
  SUM(_metrics.NumAddedPostCloneDetectionNoComments) as NumAddedPostCloneDetectionNoComments,
  SUM(_metrics.NumDeletedPostCloneDetectionNoComments) as NumDeletedPostCloneDetectionNoComments,
  SUM(_metrics.NumAddedPostCloneDetectionNoComments) + SUM(_metrics.NumDeletedPostCloneDetectionNoComments) as NumPostCloneNoCommentsTotal,
  
  SUM(_metrics.NumAddedClonedBlockLines) as NumAddedClonedBlockLines,
  SUM(_metrics.NumDeletedClonedBlockLines) as NumDeletedClonedBlockLines,
  SUM(_metrics.NumAddedClonedBlockLines) + SUM(_metrics.NumDeletedClonedBlockLines) as NumClonedBlockLinesTotal,
  SUM(_metrics.NumAddedClonedBlockLinesNoComments) as NumAddedClonedBlockLinesNoComments,
  SUM(_metrics.NumDeletedClonedBlockLinesNoComments) as NumDeletedClonedBlockLinesNoComments,
  SUM(_metrics.NumAddedClonedBlockLinesNoComments) + SUM(_metrics.NumDeletedClonedBlockLinesNoComments) as NumClonedBlockLinesNoCommentsTotal

      FROM  repository_entity AS _repo
INNER JOIN  projects as _project ON _repo.Project_id = _project.AI_ID
INNER JOIN  commit_entity AS _commit ON _repo.ID = _commit.Repository_id
INNER JOIN  tree_entry_contribution_entity AS _contrib ON _commit.ID = _contrib.Commit_id
INNER JOIN  tree_entry_changes_entity _tree ON _tree.ID = _contrib.TreeEntryChanges_id
INNER JOIN  hours_entity as _hours ON _hours.ID = _contrib.Hours_id
INNER JOIN  tree_entry_changes_metrics_entity AS _metrics ON _contrib.TreeEntryChangesMetrics_id = _metrics.ID


WHERE
  (
	 /* With statuses {1,2}, the SMT can only be 0 */
   __en_ad_start__(_tree.Status = 1 OR _tree.Status = 2)
   OR__en_ad_end__
   (_contrib.SimilarityMeasurementType = __smt__ AND (_tree.Status = 3 OR _tree.Status = 4))
  )

GROUP BY _commit.ID
) foo
WHERE foo.Hours >= __min_hours__ AND foo.Hours <= __max_hours__;