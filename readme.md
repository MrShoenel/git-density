# Git Density

Git Density (`git-density`) is a tool to analyze `git`-repositories with the goal of detecting the source code density.

It was developed during the research phase of the short technical paper and poster "_A changeset-based approach to assess source code density and developer efficacy_" [1].

## Building and running

To build the application, restore all _nuget_ packages and simply rebuild all projects.

Run `GitDensity.exe`, which has an exhaustive command line interface for analyzing repositories. This implementation also includes a reimplementation of `git-hours` [2], runnable using `GitHours.exe` (with a similar command line interface).

## Requirement of clone detection

This application relies on an external executable to run clone detection. Currently, it uses a local version of Softwerk's clone detection service [3]. To obtain a copy of this tool, please contact welf.lowe@lnu.se.

## Caveats

If using `MySQL`, the latest 5.7.x GA-releases work, while some of the 8.x versions appear to cause problems in conjunction with Fluent NHibernate. You may also use other types of databases, as Git Density supports these: MsSQL2000, MsSQL2005, MsSQL2008, MsSQL2012, MySQL, Oracle10, Oracle9, PgSQL81, PgSQL82, SQLite, SQLiteTemp.

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
