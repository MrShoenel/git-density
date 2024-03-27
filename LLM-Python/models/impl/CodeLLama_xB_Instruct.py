"""
Author: Sebastian Hönel
"""

from models.ModelBase import ModelBase
from transformers import AutoTokenizer, AutoModelForCausalLM
import torch
from enum import StrEnum


class CodeLLama_Sizes(StrEnum):
    _7B = '7b'
    _13B = '13b'
    _34B = '34b'
    _70B = '70b'


class CodeLLama_xB_Instruct(ModelBase):
    """
    Implementation of the CodeLLama Instruct model in 4 different sizes.
    """
    def __init__(self, size: CodeLLama_Sizes, dev: torch.device = 'cuda') -> None:
        super().__init__(dev)
        self.size = size
        self.dev = dev
        self.model_id = f'codellama/CodeLlama-{str(size)}-Instruct-hf'
        self.tokenizer = AutoTokenizer.from_pretrained(self.model_id)
        self.model = AutoModelForCausalLM.from_pretrained(self.model_id, torch_dtype=torch.float16, device_map='auto')

    
    def prompt(self, prompt: str, include_rules: bool = True, **kwargs) -> str:
        sp = self.split_prompt(prompt=prompt)
        system = sp.Instructions
        if include_rules:
            system += f'\n\nRules\n=====\n\n{sp.Rules}\n\n'
        system += f'The Summary of the Commit\n=========================\n\n{sp.Summary}\n\nThe Commit\'s affected files\n===========================\n\n{sp.AffectedFiles}'

        messages = [
            {"role": "system", "content": system },
            {"role": "user", "content": "Report the percentage to which the given commit can be assigned to each maintenance activity in percent."}]
        
        inputs = self.tokenizer.apply_chat_template(messages, return_tensors='pt').to(device=self.dev)

        
        inputs = self.tokenizer.apply_chat_template(messages, return_tensors='pt').to(device=self.dev)
        output = self.model.generate(input_ids=inputs, max_new_tokens=200)
        output = output[0].to('cpu')
        return self.tokenizer.decode(output)