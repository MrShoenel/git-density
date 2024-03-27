import abc
from dataclasses import dataclass
from re import split
from torch import device


@dataclass
class SplitPrompt:
    Instructions: str
    Rules: str
    Summary: str
    AffectedFiles: str
    Result: str


class ModelBase:
    def __init__(self, dev: device = 'cuda') -> None:
        self.dev = dev
        self.model_id: str = None

    @abc.abstractmethod
    def prompt(self, prompt: str, include_rules: bool=True, **kwargs) -> str:
        return
    
    def split_prompt(self, prompt: str) -> SplitPrompt:
        """
        We expect a prompt that contains these sections:
        First the actual instructions, followed by "Rules" (underlined
        with =), followed by "The Commit's affected files" (also underlined),
        followed by "The Summary of the Commit" (also underlined),
        followed by "The Commit's affected files" (also underlined),
        followed by "Result" (also underlined).

        In this method, we split this prompt apart so that each model
        may compose a more suitable prompt from these elements.
        """

        instructions, rules_and_more = split(pattern=r'Rules\n=====', string=prompt)
        rules, summary_and_more = split(pattern=r'The Summary of the Commit\n=========================', string=rules_and_more)
        summary, affected_and_more = split(pattern=r"The Commit's affected files\n===========================", string=summary_and_more)
        affected, result = split(pattern=r'Result\n======', string=affected_and_more)

        return SplitPrompt(Instructions=instructions.strip(), Rules=rules.strip(), Summary=summary.strip(), AffectedFiles=affected.strip(), Result=result.strip())

