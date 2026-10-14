import sys
import unittest
from pathlib import Path

sys.path.insert(0,str(Path(__file__).resolve().parent.parent))
import random_stream_reference as reference

class RandomStreamReference(unittest.TestCase):
    """The same golden values as tests/Sovereigns.Simulation.Tests/RandomStreamTests.cs, from the ADR-0007 definition."""

    def test_golden_values(self):
        key=reference.stream_key(20261014,'genesis.rivers',7,42)
        self.assertEqual(key,0x348619051c7a2cd4)
        self.assertEqual([reference.draw(key,i) for i in range(3)],[0x905d30fcafb27273,0x5c59462207abf377,0xe28c47e47e364887])
        self.assertEqual(reference.draw(reference.stream_key(1,'calendar.test'),0),0xa3643ca19fa7afee)

if __name__=='__main__': unittest.main()
