# ChineseFountain
C# translation of https://github.com/chrisdew/chinese_fountain

This adds an internal big-int library (which needs improving), and
a wrapping protocol which allows data to be recovered even when
delivered out-of-order or with a subset of corrupted packets.