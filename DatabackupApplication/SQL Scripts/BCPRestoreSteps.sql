----drop constraints

DECLARE @sql NVARCHAR(MAX),@tableName NVARCHAR(MAX);
SET @sql = N'';
SET @tableName ='Blog';


SELECT @sql = @sql + N'
  ALTER TABLE ' + QUOTENAME(cs.name) + '.' + QUOTENAME(ct.name) 
    + ' DROP CONSTRAINT ' + QUOTENAME(fk.name) + ';'
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS ct
  ON fk.parent_object_id = ct.[object_id]
INNER JOIN sys.schemas AS cs 
  ON ct.[schema_id] = cs.[schema_id]
 where fk.referenced_object_id = (select object_id 
                               from sys.tables 
                               where name = 'Blog')
							   or
		fk.parent_object_id = (select object_id 
                               from sys.tables 
                               where name = 'Blog');

PRINT @sql;

--use Blogging
--select *
--FROM sys.foreign_keys AS fk
--INNER JOIN sys.tables AS ct
--  ON fk.parent_object_id = ct.[object_id]
--INNER JOIN sys.schemas AS cs 
--  ON ct.[schema_id] = cs.[schema_id]
-- where fk.referenced_object_id = (select object_id 
--                               from sys.tables 
--                               where name = 'Post')
--or
--		fk.parent_object_id = (select object_id 
--                               from sys.tables 
--                               where name = 'Post');




--select * from sys.tables;
--  ALTER TABLE [dbo].[Post] DROP CONSTRAINT [FK_Post_Blog_BlogId];
--  --truncate table to be 
--  --TRUNCATE table blogging..BLOG;
--  TRUNCATE table blogging..POST;
----!!bcp Blogging..Blog in D:\\Backup\\Blog.bcp -c -T  -S .\SQLExpress -U sa -P 123456
--select * from Blogging..Blog   
--select * from Blogging..Post   



ALTER TABLE [POST] ADD CONSTRAINT [FK_Post_Blog_BlogID] FOREIGN KEY ([BlogId]) REFERENCES [Blog] ([BlogId]) ON DELETE CASCADE;

--!!bcp Blogging..Post in D:\\Backup\\Post.bcp -c -T  -S .\SQLExpress -U sa -P 123456





DECLARE @sql2 NVARCHAR(MAX);
DECLARe @tablename nvarchar(MAX)='blog';
SET @sql2 = N'';


SELECT @sql2 = @sql2 + N'
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
WHERE rt.is_ms_shipped = 0 AND ct.is_ms_shipped = 0
and
 ( fk.referenced_object_id = (select object_id 
                               from sys.tables 
                               where name = @tablename)
or
		fk.parent_object_id = (select object_id 
                               from sys.tables 
                               where name = @tablename));

PRINT @sql2;