"""
Author: Sebastian Hönel
"""

from models.ModelBase import ModelBase
import torch
import transformers
from transformers import LlamaForCausalLM, LlamaTokenizer
from enum import StrEnum

class LLama2_Sizes(StrEnum):
    _7B = '7b'
    _13B = '13b'
    _70B = '70b'


class LLama2_xB_Chat(ModelBase):
    """
    Class for the family of LLama2 models. Supports 7B, 13B, and 70B size models.
    """
    def __init__(self, size: LLama2_Sizes, dev: torch.device = 'cuda') -> None:
        super().__init__(dev)
        self.size = size
        model_id = f'meta-llama/Llama-2-{str(size)}-chat-hf'
        model_config = transformers.AutoConfig.from_pretrained(model_id)
        self.model = LlamaForCausalLM.from_pretrained(
            model_id, trust_remote_code=True, config=model_config, device_map='auto')
        self.model.eval()

        self.tokenizer = LlamaTokenizer.from_pretrained(model_id)
        self.pipeline = transformers.pipeline(task="text-generation", model=self.model, tokenizer=self.tokenizer, torch_dtype=torch.float16, device_map="auto")
    

    def prompt(self, prompt: str, include_rules: bool = True, **kwargs) -> str:
        if not include_rules:
            raise Exception('This model does not work correctly without rules.')
        sp = self.split_prompt(prompt=prompt)

        use_prompt = f'{sp.Instructions}\n\nRules\n====={sp.Rules}\n\nThe Summary of the Commit\n=========================\n\n{sp.Summary}\n\nThe Commit\'s affected files\n===========================\n\n{sp.AffectedFiles}\n\nResult\n======Report the percentage to which the given commit should be assigned to each maintenance activity in percent.';

        sequences = self.pipeline(
            use_prompt, do_sample=True, top_k=10, num_return_sequences=1,
            eos_token_id=self.tokenizer.eos_token_id, max_new_tokens=10000,
            temperature=.3, # default is 0.6
            top_p=.3) # default is 0.9
        
        result: str = sequences[0]['generated_text']
        result = result.replace(use_prompt, '').strip()
        return result


