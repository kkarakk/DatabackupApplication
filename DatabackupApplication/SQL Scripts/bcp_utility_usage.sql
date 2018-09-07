--https://dba.stackexchange.com/questions/15682/sql-server-2012-backward-compatibility-for-backups-with-2008/102858#102858




--SELECT 
--   'bcp SOURCEDATABASE.' + s.Name + '.' + t.NAME  + ' out d:\dbdump\' + s.Name + '.' + t.NAME  + '.dat -N -S .\SQLEXPRESS\ -Usa -P123456'
	--    'bcp DESTINATIONDATASE.' + s.Name + '.' + t.NAME  + ' in d:\dbdump\' + s.Name + '.' + t.NAME  + '.dat -N -S DESTINATIONSERVER\INSTANCE -UUSER -PPASSWORD -E -h TABLOCK -b 1000 -e d:\dbdump\' + s.Name + '.' + t.NAME  + '.ERRORS.dat'
--FROM 
--    sys.tables t
--INNER JOIN      
--    sys.indexes i ON t.OBJECT_ID = i.object_id
--LEFT OUTER JOIN 
--    sys.schemas s ON t.schema_id = s.schema_id
--ORDER BY 
--    s.Name, t.NAME


SELECT 'bcp SOURCEDATABASE.' + t.TABLE_NAME + ' out d:\dbdump\' + '.' + t.TABLE_NAME + '.dat -N -S .\SQLEXPRESS\ -Usa -P123456'
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_TYPE = 'BASE TABLE'
AND TABLE_CATALOG='Blogging';

select *
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_TYPE = 'BASE TABLE'
AND TABLE_CATALOG='Blogging';

select *from blogging..Blog
--SQLCMD MODE ENABLE
use blogging
!!bcp "select * FROM INFORMATION_SCHEMA.TABLES ;" queryout D:\authors.txt -S .\SQLExpress -U sa -P 123456 -c

!! exec xp_cmdshell bcp "select * FROM INFORMATION_SCHEMA.TABLES ;" queryout D:\authors.txt -S .\SQLExpress -U sa -P 123456 -c

!!bcp blogging in d:\blogging.bak 


USE BLOGGING;
SELECT * FROM blogging.INFORMATION_SCHEMA.TABLES where table_name = 'blog';
USE nopdatabase;
select * from information_schema.tables;

use blogging;
select * from blogging..blog;
!!bcp blogging..blog out d:\backup.bcp -c -T -S .\SQLExpress -U sa -P 123456 -c

!!backup database blogging to disk='d:\backup\bloggingDifferential.bak' WITH DIFFERENTIAL;

!!bcp blogging..blog out d:\blog.bcp -c -T -S .\SQLExpress -U sa -P 123456 

select * from blogging..blog;
RESTORE HEADERONLY FROM
     DISK = 'd:\backup\BloggingDatabaseBackup.bak'

	 
RESTORE HEADERONLY FROM
     DISK = 'd:\backup\bloggingDifferential.bak'
--WHATS YOUR SERVER NAME
SELECT @@SERVERNAME