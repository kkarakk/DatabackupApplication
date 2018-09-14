----https://dba.stackexchange.com/questions/15682/sql-server-2012-backward-compatibility-for-backups-with-2008/102858#102858




----SELECT 
----   'bcp SOURCEDATABASE.' + s.Name + '.' + t.NAME  + ' out d:\dbdump\' + s.Name + '.' + t.NAME  + '.dat -N -S .\SQLEXPRESS\ -Usa -P123456'
--	--    'bcp DESTINATIONDATASE.' + s.Name + '.' + t.NAME  + ' in d:\dbdump\' + s.Name + '.' + t.NAME  + '.dat -N -S DESTINATIONSERVER\INSTANCE -UUSER -PPASSWORD -E -h TABLOCK -b 1000 -e d:\dbdump\' + s.Name + '.' + t.NAME  + '.ERRORS.dat'
----FROM 
----    sys.tables t
----INNER JOIN      
----    sys.indexes i ON t.OBJECT_ID = i.object_id
----LEFT OUTER JOIN 
----    sys.schemas s ON t.schema_id = s.schema_id
----ORDER BY 
----    s.Name, t.NAME


--SELECT 'bcp SOURCEDATABASE.' + t.TABLE_NAME + ' out d:\dbdump\' + '.' + t.TABLE_NAME + '.dat -N -S .\SQLEXPRESS\ -Usa -P123456'
--FROM INFORMATION_SCHEMA.TABLES t
--WHERE t.TABLE_TYPE = 'BASE TABLE'
--AND TABLE_CATALOG='Blogging';

--select *
--FROM INFORMATION_SCHEMA.TABLES t
--WHERE t.TABLE_TYPE = 'BASE TABLE'
--AND TABLE_CATALOG='Blogging';

--select *from blogging..Blog
----SQLCMD MODE ENABLE
--use blogging
--!!bcp "select * FROM INFORMATION_SCHEMA.TABLES ;" queryout D:\authors.txt -S .\SQLExpress -U sa -P 123456 -c

--!! exec xp_cmdshell bcp "select * FROM INFORMATION_SCHEMA.TABLES ;" queryout D:\authors.txt -S .\SQLExpress -U sa -P 123456 -c

--!!bcp blogging in d:\blogging.bak 


--USE BLOGGING;
--SELECT * FROM blogging.INFORMATION_SCHEMA.TABLES where table_name = 'blog';
--USE nopdatabase;
--select * from information_schema.tables;

--use blogging;
--select * from blogging..blog;
--!!bcp blogging..blog out d:\backup.bcp -c -T -S .\SQLExpress -U sa -P 123456 -c

--!!backup database blogging to disk='d:\backup\bloggingDifferential.bak' WITH DIFFERENTIAL;

--!!bcp blogging..blog out d:\blog.bcp -c -T -S .\SQLExpress -U sa -P 123456 

--!!bcp Blogging..Blog out D:\\Backup\\Blog.bcp -h "CHECK_CONSTRAINTS"  -n -T -S .\SQLExpress -U sa -P 123456

--!!bcp Blogging..Blog in D:\\Backup\\Blog.bcp -n -T  -S .\SQLExpress -U sa -P 123456

--select * from blogging..blog;
--TRUNCATE table blogging..BLOG;
--update blogging..blog set url = 'http://helloworld.com'  where blogId=3;
--RESTORE HEADERONLY FROM
--     DISK = 'd:\backup\BloggingDatabaseBackup.bak'


	 
--RESTORE HEADERONLY FROM
--     DISK = 'd:\backup\bloggingDifferential.bak'
----WHATS YOUR SERVER NAME
--SELECT @@SERVERNAME

--TRUNCATE table blogging..BLOG;
--!!bcp blogging..blog format nul -c -x -f D:\\Backup\\blogFormat-c.xml  -T -S .\SQLExpress -U sa -P 123456   

select * from #x
CREATE TABLE #x -- feel free to use a permanent table
(
  drop_script NVARCHAR(MAX),
  create_script NVARCHAR(MAX)
);
  
DECLARE @drop   NVARCHAR(MAX) = N'',
        @create NVARCHAR(MAX) = N'';

-- drop is easy, just build a simple concatenated list from sys.foreign_keys:
SELECT @drop += N'
ALTER TABLE ' + QUOTENAME(cs.name) + '.' + QUOTENAME(ct.name) 
    + ' DROP CONSTRAINT ' + QUOTENAME(fk.name) + ';'
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS ct
  ON fk.parent_object_id = ct.[object_id]
INNER JOIN sys.schemas AS cs 
  ON ct.[schema_id] = cs.[schema_id];

INSERT #x(drop_script) SELECT @drop;

-- create is a little more complex. We need to generate the list of 
-- columns on both sides of the constraint, even though in most cases
-- there is only one column.
SELECT @create += N'
ALTER TABLE ' 
   + QUOTENAME(cs.name) + '.' + QUOTENAME(ct.name) 
   + ' ADD CONSTRAINT ' + QUOTENAME(fk.name) 
   + ' FOREIGN KEY (' + STUFF((SELECT ',' + QUOTENAME(c.name)
   -- get all the columns in the constraint table
    FROM sys.columns AS c 
    INNER JOIN sys.foreign_key_columns AS fkc 
    ON fkc.parent_column_id = c.column_id
    AND fkc.parent_object_id = c.[object_id]
    WHERE fkc.constraint_object_id = fk.[object_id]
    ORDER BY fkc.constraint_column_id 
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'')
  + ') REFERENCES ' + QUOTENAME(rs.name) + '.' + QUOTENAME(rt.name)
  + '(' + STUFF((SELECT ',' + QUOTENAME(c.name)
   -- get all the referenced columns
    FROM sys.columns AS c 
    INNER JOIN sys.foreign_key_columns AS fkc 
    ON fkc.referenced_column_id = c.column_id
    AND fkc.referenced_object_id = c.[object_id]
    WHERE fkc.constraint_object_id = fk.[object_id]
    ORDER BY fkc.constraint_column_id 
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'') + ');'
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS rt -- referenced table
  ON fk.referenced_object_id = rt.[object_id]
INNER JOIN sys.schemas AS rs 
  ON rt.[schema_id] = rs.[schema_id]
INNER JOIN sys.tables AS ct -- constraint table
  ON fk.parent_object_id = ct.[object_id]
INNER JOIN sys.schemas AS cs 
  ON ct.[schema_id] = cs.[schema_id]
WHERE rt.is_ms_shipped = 0 AND ct.is_ms_shipped = 0;

UPDATE #x SET create_script = @create;

PRINT @drop;
PRINT @create;

DECLARE @sql NVARCHAR(MAX);
SET @sql = N'';

SELECT @sql = @sql + N'
  ALTER TABLE ' + QUOTENAME(s.name) + N'.'
  + QUOTENAME(t.name) + N' DROP CONSTRAINT '
  + QUOTENAME(c.name) + ';'
FROM sys.objects AS c
INNER JOIN sys.tables AS t
ON c.parent_object_id = t.[object_id]
INNER JOIN sys.schemas AS s 
ON t.[schema_id] = s.[schema_id]
WHERE c.[type] IN ('D','C','F','PK','UQ')
ORDER BY c.[type];

PRINT @sql;

--/*
--EXEC sp_executesql @drop
---- clear out data etc. here
--EXEC sp_executesql @create;
--*/

use blogging;
SELECT  
     KCU1.CONSTRAINT_NAME AS FK_CONSTRAINT_NAME 
    ,KCU1.TABLE_NAME AS FK_TABLE_NAME 
    ,KCU1.COLUMN_NAME AS FK_COLUMN_NAME 
    ,KCU1.ORDINAL_POSITION AS FK_ORDINAL_POSITION 
    ,KCU2.CONSTRAINT_NAME AS REFERENCED_CONSTRAINT_NAME 
    ,KCU2.TABLE_NAME AS REFERENCED_TABLE_NAME 
    ,KCU2.COLUMN_NAME AS REFERENCED_COLUMN_NAME 
    ,KCU2.ORDINAL_POSITION AS REFERENCED_ORDINAL_POSITION 
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS RC 

INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU1 
    ON KCU1.CONSTRAINT_CATALOG = RC.CONSTRAINT_CATALOG  
    AND KCU1.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA 
    AND KCU1.CONSTRAINT_NAME = RC.CONSTRAINT_NAME 

INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU2 
    ON KCU2.CONSTRAINT_CATALOG = RC.UNIQUE_CONSTRAINT_CATALOG  
    AND KCU2.CONSTRAINT_SCHEMA = RC.UNIQUE_CONSTRAINT_SCHEMA 
    AND KCU2.CONSTRAINT_NAME = RC.UNIQUE_CONSTRAINT_NAME 
    AND KCU2.ORDINAL_POSITION = KCU1.ORDINAL_POSITION 

