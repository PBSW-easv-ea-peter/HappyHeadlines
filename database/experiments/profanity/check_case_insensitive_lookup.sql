-- Mirrors the lookup used by ProfanityRepository.cs: words are lowercased in C# before the
-- query, so banned_words matches are case-sensitive at the SQL level by design. This confirms
-- an uppercase variant of a banned word only matches once lowercased.
select word from banned_words where word = any(array['idiot', 'stupid', 'dumb']);   -- expect 3 rows
select word from banned_words where word = any(array['IDIOT', 'Stupid', 'DUMB']);   -- expect 0 rows (case-sensitive)
