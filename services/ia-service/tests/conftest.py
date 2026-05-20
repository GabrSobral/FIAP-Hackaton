"""
Stub out heavy ML dependencies so tests run without GPU/model weights installed.
This must execute before any app module is imported, which pytest guarantees
by loading conftest.py before collecting tests.
"""

import sys
from unittest.mock import MagicMock

_HEAVY_DEPS = [
    "torch",
    "transformers",
    "transformers.generation",
    "PIL",
    "PIL.Image",
    "accelerate",
    "qwen_vl_utils",
]
for _mod in _HEAVY_DEPS:
    if _mod not in sys.modules:
        sys.modules[_mod] = MagicMock()
