--database restore query
use Master;
DECLARE @ErrorMessage NVARCHAR(4000);
 ALTER DATABASE 
 Blogging SET SINGLE_USER WITH ROLLBACK IMMEDIATE;    
 BEGIN TRY   
     RESTORE DATABASE Blogging FROM DISK = 'D:\Backup\BloggingFullDatabaseBackup.bak' WITH REPLACE;    
 END TRY   
 BEGIN CATCH    
     SET @ErrorMessage = ERROR_MESSAGE();    
 END CATCH    
 ALTER DATABASE Blogging SET MULTI_USER WITH ROLLBACK IMMEDIATE    
 IF (@ErrorMessage is not NULL)    
 BEGIN    
     RAISERROR (@ErrorMessage, 16, 1)    
 END

 use Blogging;
 SELECT * FROM fn_dblog(NULL, NULL);

 select [Current LSN],
       [Operation],
       [Transaction Name],
       [Transaction ID],
       [Transaction SID],
       [SPID],
       [Begin Time]
FROM   fn_dblog(null,null)



SELECT  [name],
        [log_reuse_wait] ,
        [log_reuse_wait_desc]
FROM    [sys].[databases]   
where [name] = Blogging;