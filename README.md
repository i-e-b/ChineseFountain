# ChineseFountain
C# translation of https://github.com/chrisdew/chinese_fountain

This adds an internal big-int library (which needs improving), and
a wrapping protocol which allows data to be recovered even when
delivered out-of-order or with a subset of corrupted packets.

Some performance improvements are made, making decoding significantly
faster than encoding. The algorithm is sufficiently fast for 4kb
blocks, but seriously struggles at 1MB, as the encode stage scales
at around O(n^2).