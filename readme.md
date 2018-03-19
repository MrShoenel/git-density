# Git Density

Git Density (`git-density`) is a tool to analyze `git`-repositories with the goal of detecting the source code density.

It was developed during the research phase of the short technical paper and poster "_A changeset-based approach to assess source code density and developer efficacy_" [1].

## Building and running

To build the application, restore all _nuget_ packages and simply rebuild all projects.

Run `GitDensity.exe`, which has an exhaustive command line interface for analyzing repositories. This implementation also includes a reimplementation of `git-hours` [2], runnable using `GitHours.exe` (with a similar command line interface).

## Requirement of clone detection

This application relies on an external executable to run clone detection. Currently, it uses a local version of Softwerk's clone detection service [3]. To obtain a copy of this tool, please contact welf.lowe@lnu.se.

___



[1] S. Hönel, M. Ericsson, W. Löwe, and A. Wingkvist, "Poster and 2 pp paper: A changeset-based approach to assess source code density and developer efficacy." _Proceedings of International Conference of Software Engineering (ICSE)_, 2018 (To appear), https://www.icse2018.org/event/icse-2018-posters-poster-a-changeset-based-approach-to-assess-source-code-density-and-developer-efficacy

[2] Git hours. "Estimate time spent on a Git repository." https://github.com/kimmobrunfeldt/git-hours

[3] QTools Clone Detection. http://qtools.se/
