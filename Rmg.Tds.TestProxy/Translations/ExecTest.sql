DECLARE @NVTest varchar(255) = 'foo';

-- Match
EXEC TestNVarchar @NVTest;
-- End

-- Replace
SELECT 'Test replacement ' + @NVTest;
-- End