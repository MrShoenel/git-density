"""
Author: Sebastian Hönel
"""
from models.ModelBase import ModelBase
from transformers import AutoTokenizer, AutoModelForCausalLM
import torch



class Gemma_7B_Instruct(ModelBase):
    def __init__(self, dev: torch.device = 'cuda') -> None:
        super().__init__(dev)
        self.model_id = 'google/gemma-7b-it'
        self.tokenizer = AutoTokenizer.from_pretrained(self.model_id, token=True)
        self.model = AutoModelForCausalLM.from_pretrained(self.model_id, torch_dtype=torch.bfloat16,attn_implementation="flash_attention_2", token=True).to(device=dev)
    

    def prompt(self, prompt: str, include_rules: bool = True, **kwargs) -> str:
        sp = self.split_prompt(prompt=prompt)
        use_prompt = f'{sp.Instructions}\n\nRules\n====={sp.Rules}\n\nThe Summary of the Commit\n=========================\n\n{sp.Summary}\n\nThe Commit\'s affected files\n===========================\n\n{sp.AffectedFiles}\n\nResult\n======Report the percentage to which the given commit should be assigned to each maintenance activity in percent.';

        input_ids = self.tokenizer(use_prompt, return_tensors='pt').to(self.dev)
        outputs = self.model.generate(**input_ids, max_new_tokens=200)
        return self.tokenizer.decode(outputs[0])
