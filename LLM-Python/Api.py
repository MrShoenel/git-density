from models.ModelBase import ModelBase
from models.impl.Mistral_8x7B_Instruct_v01 import Mistral8x7BInstructV01
from models.impl.LLama2_xB_Chat import LLama2_xB_Chat
from models.impl.CodeLLama_xB_Instruct import CodeLLama_xB_Instruct
from torch import device
from typing import Mapping

from threading import Semaphore
from asyncio import Future, get_event_loop


class Api:
    def __init__(self, use_device: device = 'cuda') -> None:
        self.dev = use_device
        self.models: Mapping[str, ModelBase] = dict()
        self.locks: Mapping[str, Semaphore] = dict([(m, Semaphore(value=1)) for m in self.available_models])
    
    @property
    def available_models(self) -> list[str]:
        return [Mistral8x7BInstructV01.__name__, LLama2_xB_Chat.__name__, CodeLLama_xB_Instruct.__name__]
    
    def _get_model(self, model: str, **kwargs) -> ModelBase:
        s = self.available_models
        if not model in s:
            raise Exception(f'The model "{model}" is not known.')
        
        if not model in self.models.keys():
            # Instantiate model.
            self.models[model] = globals()[model](dev=self.dev, **kwargs)
        
        return self.models[model]
    

    def prompt_model(self, model: str, prompt: str) -> Future:
        future = Future()

        async def prompt_model():
            got_lock = False
            try:
                got_lock = self.locks[model].acquire(blocking=True)
                instance = self._get_model(model=model)
                future.set_result(instance.prompt(prompt=prompt))
            except Exception as ex:
                future.set_exception(ex)
            finally:
                if got_lock:
                    self.locks[model].release()
        
        get_event_loop().create_task(prompt_model())
        
        return future
