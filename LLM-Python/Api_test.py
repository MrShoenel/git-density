from .Api import Api
from .models.ModelBase import ModelBase


prompt = """
You are to help with the classification of commits from source code repositories. You may or may not be given additional rules below that you can apply for the purpose of classification.
You are to assign commits probabilistically (i.e., express the class membership as percentage) to the three maintenance categories
**adaptive**, **corrective**, and **perfective**. You are to consider the commit message, as well as the underlying source code.
Adaptive changes are those that add functionality. Corrective changes are those that fix a fault or correct a behavior. Perfective
changes are made to accommodate future features, but do not alter the external application behavior.


Rules
=====

1.  Changing code to (not) throw an exception is considered
    **corrective**.

2.  Application code has precedence over test-code (i.e. the label ought
    to be determined by the application code first). If a e.g. a fix in
    app-code was made and unit tests were added (rule #3, perfective),
    the label would still be corrective.

    A.  However, if the altered application code is considerably smaller
        in size and the rest of the changes are predominantly of a
        different change-type, then that label ought to be used.

3.  On very low confidence (do not forget to search for tickets using
    SHA1 and try keyword analysis), skip commit. This should probably
    also be applied to very large/tangled commits.

4.  If existing behavior is fixed to e.g. (not) cause an Exception/Error
    (usually w/o unit tests altered), then this is considered
    **corrective**. Also true for when the test(s) have not been altered
    but do pass now (i.e. non-passing test) or when tests have been
    altered to accomodate the corrected behavior.

    A.  If the corrective commit was made to *tighten* the code, then my
        rule #7 need to be considered.

5.  Rule #3 still applies if unit-tests were removed, if those removed
    parts can/should be considered now obsolete and are presumable
    superseded by the new test code.

6.  When **only** removing obsolete code resulting in required
    refactorings should be considered **perfective**.

    A.  Commits that merge functionality, without being actual
        merge-commits, should be considered to be **perfective**.

    B.  If existing code was **replaced** (subtle changes) to accomodate
        a new feature (e.g. adding a parameter in constructor and
        adapting the initialization of variables), then the change may
        be considered **adaptive**.

7.  *Tightening* of code, such as introducing generics or altering them
    or restricting the visibility of e.g. constructors, can be
    considered **perfective**, as it improves the code.

    A.  If a *seemingly* (i.e. not *outright*) corrective commit alters
        the code's behavior to *tighten* it (e.g. by now properly
        handling an NPE or by removing non-deterministic behavior), then
        this rule should be applied.

    B.  Improving of unit-tests should be considered **perfective**.

    C.  Modifications to code with concern to potential future bugs (and
        without a ticket) should also be considered **perfective**.

    D.  If the commit is *seemingly* tightening, but leaves the previous
        functionality in place (e.g. through overriding), then it may be
        considered **adapting**, as it add a new API.

8.  If new performance-tests were added, the commit is to be treated as
    if it was new unit tests (rule #3).

9.  Always try to analyze the message using the manual commit
    classification helper, to see if there is a clear indication.

    A.  If all the scores are zero, attempt to replace words with
        synonyms that are actually matched, e.g. "enable" is a synonym
        for "allow".

10. If only renamings were made **and** the code cannot be said to be
    either corrective or adaptive, it should be **perfective**. In case
    of tangled commits, where my rule #4 would normally apply, this rule
    has **precedence**.

11. **Loosening** or **exposing** of code in shape of removing/loosening
    of restrictions, such as exposing code by making it *public*, may be
    considered **adaptive**, as it "adds" the feature by exposing it,
    such that it appears to be a new feature.

12. Rule #1 has low precedence, if the code changes were equal to or
    greater in size. In that case, the changes to the code need to be
    evaluated.

    A.  Updating *Javadoc* or *comments* of Rule #1 shall refer only to
        code-accompanying comments, and **not** to other information to
        be found occasionally in code, such as license headers.

13. Refactorings, such as consolidation of functionalities, that do not
    correct bugs and do not introduce new functionality (except for
    functionality necessary to accomodate the refactoring), are to be
    considered **perfective**.

14. Reverting the changes made by a commit demands a proper
    investigation to its nature, especially if it reverts an already
    labelled commit (i.e. one cannot necessarily use the same label).


The Summary of the Commit
=========================

The message of this commit was: ["Allow writing output to console instead of file"]. In this commit, a total of 0 files were added, 0 files were removed, 3 files were modified, and 0 files were renamed. In total, 14 lines of code were added, and 10 lines of code were removed. What follows is a summary for each added/changed/removed/renamed file. The markup [..][/..], [++][/++], and [--][/--] encloses an unchanged, added, or removed line, respectively.



The Commit's affected files
===========================

In the file GitTools/Analysis/ExtendedAnalyzer/ExtendedAnalyzer.cs, the following changes were made:

-----
Changes in lines 71 to 77:
-----
[..] #region Nominal Signatures [/..]
[..] /// <summary> [/..]
[--] /// Will map each <see cref="Signature"/> uniquely to a <see cref="DeveloperEntity"/>. [/--]
[++] /// Will map each <see cref="LibGit2Sharp.Signature"/> uniquely to a <see cref="DeveloperEntity"/>. [/++]
[..] /// Then, for each entity, a unique ID (within the current repository) is assigned. The [/..]
[..] /// IDs look like Excel columns. [/..]
[..] /// </summary> [/..]

In the file GitTools/Program.cs, the following changes were made:

-----
Changes in lines 175 to 194:
-----
[..] SHA1s = commits.Select(c => c.ShaShort()) [/..]
[..] }); [/..]
[--] using (var writer = File.CreateText(options.OutputFile)) [/--]
[++] using (var writer = String.IsNullOrWhiteSpace(options.OutputFile) ? [/++]
[++] Console.Out : File.CreateText(options.OutputFile)) [/++]
[..] { [/..]
[..] writer.Write(json); [/..]
[..] } [/..]
[--]  [/--]
[--] logger.LogInformation($"Wrote JSON to {options.OutputFile}"); [/--]
[++] logger.LogInformation($"Wrote JSON to {(String.IsNullOrWhiteSpace(options.OutputFile) ? "console" : options.OutputFile)}."); [/++]
[..] Environment.Exit((int)ExitCodes.OK); [/..]
[..] } [/..]
[..] #endregion [/..]
[--] using (var writer = File.CreateText(options.OutputFile)) [/--]
[++]  [/++]
[++] using (span) [/++]
[++] using (var writer = String.IsNullOrWhiteSpace(options.OutputFile) ? [/++]
[++] Console.Out : File.CreateText(options.OutputFile)) [/++]
[..] { [/..]
[..] // Now we extract some info and write it out later. [/..]
[..] var csvc = new CsvContext(); [/..]

-----
Changes in lines 198 to 205:
-----
[..] FileCultureInfo = Thread.CurrentThread.CurrentUICulture, [/..]
[..] SeparatorChar = ',', [/..]
[..] QuoteAllFields = true, [/..]
[--] EnforceCsvColumnAttribute = true [/--]
[++] EnforceCsvColumnAttribute = true, [/++]
[++] TextEncoding = System.Text.Encoding.UTF8 [/++]
[..] }; [/..]
[..] IAnalyzer<IAnalyzedCommit> analyzer = null; [/..]

-----
Changes in lines 353 to 361:
-----
[..] [Option('r', "repo-path", Required = true, HelpText = "Absolute path or HTTP(S) URL to a git-repository. If a URL is provided, the repository will be cloned to a temporary folder first, using its defined default branch. Also allows passing in an Internal-ID of a project from the database.")] [/..]
[..] public String RepoPath { get; set; } [/..]
[++] [Option('o', "out-file", Required = false, HelpText = "A path to a file to write the analysis' result to. If left unspecified, output is written to the console.")] [/++]
[++] public String OutputFile { get; set; } [/++]
[++]  [/++]
[..] [Option('t', "temp-dir", Required = false, HelpText = "Optional. A fully qualified path to a custom temporary directory. If not specified, will use the system's default. Be aware that the directory may be wiped at any point in time.")] [/..]
[..] public String TempDirectory { get; set; } [/..]

-----
Changes in lines 369 to 374:
-----
[..] [JsonConverter(typeof(StringEnumConverter))] [/..]
[..] public AnalysisType AnalysisType { get; set; } = AnalysisType.Extended; [/..]
[--] [Option('o', "out-file", Required = true, HelpText = "A path to a file to write the analysis' result to.")] [/--]
[--] public String OutputFile { get; set; } [/--]
[--]  [/--]
[..] [Option('k', "skip-size", Required = false, DefaultValue = false, HelpText = "If specified, will skip any size-related measurements in the " + nameof(ExtendedCommitDetails) + ".")] [/..]
[..] public Boolean SkipSizeInExtendedAnalysis { get; set; } [/..]

In the file Util/Logging/BaseLogger.cs, the following changes were made:

-----
Changes in lines 29 to 35:
-----
[..] public abstract class BaseLogger<T> : ILogger<T>, IDisposable [/..]
[..] { [/..]
[..] /// <summary> [/..]
[--] /// A dictionary will all runtime-types. The logger can log the current type. [/--]
[++] /// A dictionary with all runtime-types. The logger can log the current type. [/++]
[..] /// </summary> [/..]
[..] protected static Lazy<IDictionary<Type, String>> lazyLoadedTypes = new Lazy<IDictionary<Type, String>>(() => [/..]
[..] { [/..]

Result
======

Report the percentage to which the given commit should be assigned to each maintenance activity in percent.
"""

if __name__ == '__main__':
    from asyncio import run
    bm = ModelBase()
    temp = bm.split_prompt(prompt=prompt)

    async def main():
        api = Api()
        am = api.available_models
        print(am)

        result = await api.prompt_model(model=am[-1], prompt=prompt)
        print(result)
    
    run(main())