# Git Density

Git Density (`git-density`) is a tool to analyze `git`-repositories with the goal of detecting the source code density.

It was developed during the research phase of the short technical paper and poster "_A changeset-based approach to assess source code density and developer efficacy_" [1].

## Building and running

To build the application, restore all _nuget_ packages and simply rebuild all projects.

Run `GitDensity.exe`, which has an exhaustive command line interface for analyzing repositories. This implementation also includes a reimplementation of `git-hours` [2], runnable using `GitHours.exe` (with a similar command line interface).

## Requirement of external tools
This application relies on an external executable to run clone detection. Currently, it uses a local version of Softwerk's clone detection service [3]. To obtain a copy of this tool, please contact welf.lowe@lnu.se.

As for `git-metrics`, the application relies on another tool that supports currently obtaining software metrics from Java applications. Please contact me if you intend to use Git Metrics and require the tool.


# Structure of the applications
Git Density is a solution that currently features these three applications:
* __`git-density`__: A new metric to detect the density of software projects.
  * When running `git-density` on a repository, it will compute the density metric __as well as__ `git-hours` and also attempt to obtain the project's metrics at each commit using `git-metrics`.
  * Since the data produced by `git-density` is exhaustive and not plain, it must use a relational database as backend and does not support (yet) the output to file/stdout. All of its results are stored in the database for each repository.
  * It is possible to remove all previous analysis results for one repository (please refer to the command-line help).
* __`git-hours`__: A C# reimplementation of git-hours with some more features (like timespans between commits or time spent by each developer)
  * It comes also with its own command-line interface and supports `JSON`-formatted output. This useful for just analyzing the time spent on a repository.
  * `git-hours` is also part of the full analysis as run by `git-density`.
* __`git-metrics`__:  A C# wrapper around another tool that can build Java-based projects and extract common software metrics at each commit for the entire project and for files affected by the commit.
  * It comes also with its own command-line interface and supports `JSON`-formatted output (like `git-hours`).
  * It is part of the full analysis of `git-density` as well.
  * Please note that the standalone CLI interface is _not yet fully implemented_, although just minor things are missing (planned is a `JSON`-formatted output).
*	__`git-tools`__: A stand-alone application that uses some of the tools from the other projects to extract information from git repositories and stores them as __`CSV`__-files.
	*	Has its own command-line interface and supports online/offline repos and parallelization.
	*	Supports two methods currently: _Simple_ and _Extended_ (default) extraction.
	*	Does not require tools for clone-detection or metrics, as these are not extracted.
	*	Extracts __26__ features (__13__ in _Simple_-mode): `"SHA1","RepoPathOrUrl","AuthorName","CommitterName","AuthorTime","CommitterTime","MinutesSincePreviousCommit","Message","AuthorEmail","CommitterEmail","IsInitialCommit","IsMergeCommit","NumberOfParentCommits","ParentCommitSHA1s","NumberOfFilesAdded","NumberOfLinesAddedByAddedFilesNoComments","NumberOfLinesAddedByAddedFilesGross","NumberOfFilesDeleted","NumberOfLinesDeletedByDeletedFilesNoComments","NumberOfLinesDeletedByDeletedFilesGross","NumberOfFilesModified","NumberOfFilesRenamed","NumberOfLinesAddedByModifiedFiles","NumberOfLinesDeletedByModifiedFiles","NumberOfLinesAddedByRenamedFiles","NumberOfLinesDeletedByRenamedFiles"`

All applications can be run standalone, but may also be included as references, as they all feature a public API.
## Caveats

If using `MySQL`, the latest 5.7.x GA-releases work, while some of the 8.x versions appear to cause problems in conjunction with Fluent NHibernate. You may also use other types of databases, as Git Density supports these: `MsSQL2000`, `MsSQL2005`, `MsSQL2008`, `MsSQL2012`, `MySQL`, `Oracle10`, `Oracle9`, `PgSQL81`, `PgSQL82`, `SQLite`, `SQLiteTemp` (temporary database that is discarded after the analysis, mainly for testing).

___


# Citing
Please use the following BibTeX to cite this:
<pre>
@inproceedings{honel2018changeset,
  title={A changeset-based approach to assess source code density and developer efficacy},
  author={H{\"o}nel, Sebastian and Ericsson, Morgan and L{\"o}we, Welf and Wingkvist, Anna},
  booktitle={Proceedings of the 40th International Conference on Software Engineering: Companion Proceeedings},
  pages={220--221},
  year={2018},
  organization={ACM}
}
</pre>

___

# References

[1] Hönel, S., Ericsson, M., Löwe, W. and Wingkvist, A., 2018, May. A changeset-based approach to assess source code density and developer efficacy. In _Proceedings of the 40th International Conference on Software Engineering: Companion Proceedings_ (pp. 220-221). ACM, https://www.icse2018.org/event/icse-2018-posters-poster-a-changeset-based-approach-to-assess-source-code-density-and-developer-efficacy

[2] Git hours. "Estimate time spent on a Git repository." https://github.com/kimmobrunfeldt/git-hours

[3] QTools Clone Detection. http://qtools.se/
