---
affiliations: 
  - 
    index: 1
    name: "Department of Computer Science and Media Technology, Linnaeus University, Sweden"
authors: 
  - 
    affiliation: 1
    name: "Sebastian Hönel^[corresponding author]"
    orcid: 0000-0001-7937-1645
  - 
    affiliation: 1
    equal-contrib: false
    name: "Morgan Ericsson"
    orcid: 0000-0003-1173-5187
  - 
    affiliation: 1
    equal-contrib: false
    name: "Welf Löwe"
    orcid: 0000-0002-7565-3714
  - 
    affiliation: 1
    equal-contrib: false
    name: "Anna Wingkvist"
    orcid: 0000-0002-0835-823X
bibliography: refs.bib
date: "29 September 2022"
tags: 
  - C-Sharp
  - Repository mining
title: "Git Density: A Tool- and Analysis Suite for Mining Git Repositories"
---


\newcommand{\gd}{\textsf{Git Density}\xspace}
\newcommand\tightto{\!\to\!}
\newcommand\tightmapsto{\!\mapsto\!}
\newcommand{\tight}[1]{\,{#1}\,}
\newcommand{\utight}[1]{{#1}\,}


# Summary
<!-- A summary describing the high-level functionality and purpose of the software for a diverse, non-specialist audience. -->
<!-- Mention (if applicable) a representative set of past or ongoing research projects using the software and recent scholarly publications enabled by it. -->

Mining of software repositories is done in order to obtain rich digital artifacts related to software systems, projects, and software engineering.
Reasons for mining such repositories include, but are not limited to, understanding software evolution [@dambros2008], or classification of commits [@hindle2009].


\gd is the first tool to specifically mine a software metric to become known as the ``source code density''[@honel2020using, @honel2018density].



# Statement Of Need
<!-- A Statement of need section that clearly illustrates the research purpose of the software and places it in the context of related work. -->
Source code density was proposed to obtain a metric that can more accurately describe the size of software.


# Git Density Tool- and Analysis Suite
<!-- Here, we go into detail about the software -->




<!-- # Related Work
<!-- We will not have this section unless otherwise requested, because the text is already richely interspersed with references where appropriate. -->
<!-- A list of key references, including to other software addressing related needs. Note that the references should include full names of venues, e.g., journals and conferences, not abbreviations only understood in the context of a specific discipline. -->


# Acknowledgments
<!-- Acknowledgement of any financial support. -->
This work is supported by the [Linnaeus University Centre for Data Intensive Sciences and Applications (DISA)](https://lnu.se/forskning/sok-forskning/linnaeus-university-centre-for-data-intensive-sciences-and-applications) High-Performance Computing Center.


# Applications

The \gd tool- and analysis suite have been used to study and compare the source code density in comparison to traditonal methods of estimating size [@honel2018density].
The source code density as a language-agnostic metric was previously proven to be an effective predictor for maintenance activities associated with commits [@honel2020using].



# References