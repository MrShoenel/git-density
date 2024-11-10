"""
Author: Sebastian Hönel
"""

from models.ModelBase import ModelBase
from transformers import AutoModelForCausalLM, AutoTokenizer
from torch import device


class Mistral8x7BInstructV01(ModelBase):
    def __init__(self, dev: device = 'cuda') -> None:
        super().__init__(dev=dev)
        model_id = "mistralai/Mixtral-8x7B-Instruct-v0.1"
        self.tokenizer = AutoTokenizer.from_pretrained(model_id)
        self.model = AutoModelForCausalLM.from_pretrained(model_id, device_map="auto")
        self.model.eval()
    

    def prompt(self, prompt: str, include_rules: bool=True) -> str:
        sp = self.split_prompt(prompt=prompt)
        assistant = ''
        if include_rules:
            assistant += f'Rules\n=====\n\n{sp.Rules}\n\n'
        assistant += f'The Summary of the Commit\n=========================\n\n{sp.Summary}\n\nThe Commit\'s affected files\n===========================\n\n{sp.AffectedFiles}'

        messages = [
            {"role": "user", "content": sp.Instructions },
            {"role": "assistant", "content": assistant },
            {"role": "user", "content": "Report the percentage to which the given commit can be assigned to each maintenance activity in percent."}]
        
        inputs = self.tokenizer.apply_chat_template(messages, return_tensors="pt").to(device=self.dev)
        outputs = self.model.generate(inputs, max_new_tokens=100)
        return self.tokenizer.decode(outputs[0], skip_special_tokens=True)
