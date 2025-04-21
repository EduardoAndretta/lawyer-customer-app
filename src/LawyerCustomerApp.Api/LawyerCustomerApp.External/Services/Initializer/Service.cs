using Dapper;
using LawyerCustomerApp.External.Common.Responses.Error;
using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.Extensions.Configuration;

namespace LawyerCustomerApp.External.Initializer.Services;

public class Service : IInitializerService
{
    private readonly IDatabaseService _databaseService;
    private readonly IConfiguration   _configuration;
    public Service(IDatabaseService databaseService, IConfiguration configuration)
    {
        _databaseService = databaseService;
        _configuration   = configuration;
    }

    public async Task<Result> InitializeSqliteDatabase(Contextualizer contextualizer)
    {
		var resultContructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
		{
			resultContructor.SetConstructor(new NotFoundDatabaseConnectionStringError()
			{
				Status = 500
			});

			return resultContructor.Build();
        }
			
        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        await connection.Connection.ExecuteAsync(
@"
BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS tags ( 
	id                   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	name                 TEXT NOT NULL,
	CONSTRAINT unq_tags_name UNIQUE ( name )
 );

CREATE TABLE IF NOT EXISTS users ( 
	id                   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	name                 TEXT NOT NULL,
	email                TEXT NOT NULL,
	password             TEXT NOT NULL,
	register_date        TEXT DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT unq_users_email UNIQUE ( email )
 );

CREATE TABLE IF NOT EXISTS customers ( 
	id                   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	phone                TEXT,
	address              TEXT,
	private				 BOOLEAN NOT NULL DEFAULT 1,
	user_id              INTEGER NOT NULL,
	FOREIGN KEY ( user_id ) REFERENCES users( id )  
 );

CREATE TABLE IF NOT EXISTS lawyers ( 
	id                   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	phone                TEXT,
	address              TEXT,
	private				 BOOLEAN NOT NULL DEFAULT 1,
	user_id              INTEGER,
	FOREIGN KEY ( user_id ) REFERENCES users( id )  
 );

CREATE TABLE IF NOT EXISTS lawyers_tags ( 
	lawyer_id            INTEGER NOT NULL,
	tag_id               INTEGER NOT NULL,
	CONSTRAINT pk_lawyers_tags UNIQUE ( lawyer_id, tag_id ),
	FOREIGN KEY ( lawyer_id ) REFERENCES lawyers( id ),
	FOREIGN KEY ( tag_id )    REFERENCES tags( id )  
 );

CREATE TABLE IF NOT EXISTS customers_tags ( 
	customer_id          INTEGER NOT NULL,
	tag_id               INTEGER NOT NULL,
	CONSTRAINT pk_customers_tags UNIQUE ( customer_id, tag_id ),
	FOREIGN KEY ( customer_id ) REFERENCES customers( id ),
	FOREIGN KEY ( tag_id )      REFERENCES tags( id )  
 );

CREATE TABLE IF NOT EXISTS cases ( 
	id                   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	title                TEXT    NOT NULL,
	description          TEXT,   
	status               TEXT    NOT NULL DEFAULT 'openned',
	begin_date           TEXT    NOT NULL DEFAULT CURRENT_TIMESTAMP,
	end_date             TEXT,	 
	private				 BOOLEAN NOT NULL DEFAULT 1,
	user_id              INTEGER NOT NULL,
	customer_id          INTEGER,
	lawyer_id            INTEGER,
	FOREIGN KEY ( user_id )		REFERENCES users( id ),
	FOREIGN KEY ( customer_id ) REFERENCES customers( id ),
	FOREIGN KEY ( lawyer_id )   REFERENCES lawyers( id ),
	CHECK ( status IN ('openned', 'current', 'closed', 'archived', 'deleted') )
);

CREATE TABLE IF NOT EXISTS roles (
    id              INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    name            TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS user_attributes (
    id              INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    name            TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS permissions (
    id              INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    name            TEXT NOT NULL UNIQUE,
    description     TEXT
);

CREATE TABLE IF NOT EXISTS user_roles (
    user_id         INTEGER NOT NULL,
    role_id         INTEGER NOT NULL,
    PRIMARY KEY (user_id, role_id),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS permission_grants (
    id                      INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    permission_id           INTEGER NOT NULL,
    required_role_id        INTEGER NOT NULL,
    required_attribute_id   INTEGER NULL,

    FOREIGN KEY (permission_id)         REFERENCES permissions(id)     ON DELETE CASCADE,
    FOREIGN KEY (required_role_id)      REFERENCES roles(id)           ON DELETE CASCADE,
    FOREIGN KEY (required_attribute_id) REFERENCES user_attributes(id) ON DELETE CASCADE,

    UNIQUE (permission_id, required_role_id, required_attribute_id)
);

CREATE TABLE IF NOT EXISTS case_user_permissions (
    case_id         INTEGER NOT NULL,
    user_id         INTEGER NOT NULL,
    permission_id   INTEGER NOT NULL,

    attribute_id    INTEGER NULL,

    PRIMARY KEY (case_id, user_id, permission_id, attribute_id),
    FOREIGN KEY (case_id)       REFERENCES cases(id)           ON DELETE CASCADE,
    FOREIGN KEY (user_id)       REFERENCES users(id)           ON DELETE CASCADE,
    FOREIGN KEY (permission_id) REFERENCES permissions(id)     ON DELETE CASCADE,

	-- Link to the persona type
    FOREIGN KEY (attribute_id)  REFERENCES user_attributes(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS user_permission_overrides (
	user_id         INTEGER NOT NULL,
	permission_id   INTEGER NOT NULL,

	attribute_id    INTEGER NULL,

	PRIMARY KEY (user_id, permission_id, attribute_id),
	FOREIGN KEY (user_id)       REFERENCES users(id)           ON DELETE CASCADE,
	FOREIGN KEY (permission_id) REFERENCES permissions(id)     ON DELETE CASCADE,
	FOREIGN KEY (attribute_id)  REFERENCES user_attributes(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS documents ( 
	id                   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT ,
	name                 TEXT	 NOT NULL,
	type                 TEXT	 NOT NULL,
	upload_date          TEXT	 DEFAULT CURRENT_TIMESTAMP,
	archive_path         TEXT	 NOT NULL,
	case_id              INTEGER NOT NULL,
	FOREIGN KEY ( case_id ) REFERENCES cases( id ),
	CHECK ( type IN ('contract', 'petition', 'other') )
 );

CREATE TABLE IF NOT EXISTS interation ( 
	id                   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT ,
	type                 TEXT NOT NULL,
	description          TEXT,
	interation_date      TEXT DEFAULT CURRENT_TIMESTAMP,
	case_id              INTEGER NOT NULL,
	customer_id			 INTEGER NOT NULL,
	FOREIGN KEY ( case_id )		REFERENCES cases( id ),
	FOREIGN KEY ( customer_id ) REFERENCES customers( id ),
	CHECK ( type IN ('reunião', 'telefonema', 'email', 'outro') )
 );

CREATE TABLE IF NOT EXISTS payments ( 
	id                   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	valor                REAL NOT NULL,
	payment_date         TEXT DEFAULT CURRENT_TIMESTAMP,
	method               TEXT NOT NULL,
	status               TEXT DEFAULT 'pendente',
	case_id              INTEGER NOT NULL,
	customer_id          INTEGER NOT NULL,
	FOREIGN KEY ( case_id )     REFERENCES cases( id ),
	FOREIGN KEY ( customer_id ) REFERENCES users( id ),
	CHECK ( method IN ('cartão', 'dinheiro', 'transferência', 'outro') ),
	CHECK ( status IN ('pendente', 'pago', 'atrasado') )
 );

CREATE TABLE IF NOT EXISTS tokens ( 
	id						 INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
	jwt_token				 TEXT	  NOT NULL,
	jwt_token_limit_date	 DATETIME NOT NULL,
	refresh_token			 TEXT	  NOT NULL,
	refresh_token_limit_date DATETIME NOT NULL,
	invalidated				 BOOLEAN  NOT NULL DEFAULT 0,
	created_date  DATETIME   DEFAULT CURRENT_TIMESTAMP,
	user_id       INTEGER NOT NULL,
	FOREIGN KEY ( user_id ) REFERENCES users( id ) ON DELETE CASCADE 
 );

CREATE TABLE IF NOT EXISTS schedule ( 
	id                   INTEGER NOT NULL  PRIMARY KEY AUTOINCREMENT,
	schedule_date		  TEXT NOT NULL,
	type                  TEXT NOT NULL,
	status                TEXT DEFAULT 'agendado',
	case_id               INTEGER NOT NULL,
	lawyer_id			  INTEGER NOT NULL,
	customer_id           INTEGER NOT NULL,
	FOREIGN KEY ( case_id )	    REFERENCES cases( id ),
	FOREIGN KEY ( lawyer_id )   REFERENCES lawyers( id ),
	FOREIGN KEY ( customer_id ) REFERENCES customers( id ),
	CHECK ( type   IN ('reunião', 'consultoria', 'outro') ),
	CHECK ( status IN ('agendado', 'concluído', 'cancelado') )
 );

/* ========================================================================== */
/*                           PERMISSION ARCHITECTURE                          */
/* ========================================================================== */

/*
--------------------------------------------------------------------------------
|                              HOW PERMISSIONS WORK                            |
--------------------------------------------------------------------------------

This system uses a layered approach to determine if a user has permission to
perform an action, potentially specific to a data object (like a case) and
often dependent on the user's currently active 'persona' (e.g., acting as
a LAWYER or a CUSTOMER).

Core Concepts:
---------------
1.  Users: Standard user accounts (`users` table - assumed external).
2.  Roles: High-level categories users belong to (e.g., 'USER', 'ADMIN').
    Managed in `roles` and assigned via `user_roles`.
3.  Permissions: Granular actions that can be performed (e.g., 'CREATE_CASE',
    'EDIT_CASE_DETAILS', 'VIEW_ANY_CASE'). Defined in `permissions`.
4.  Attributes (Personas): Specific capacities or contexts a user can act
    within (e.g., 'LAWYER', 'CUSTOMER'). Defined in `user_attributes`.
    A user's *capability* to act as a persona is typically determined by the
    existence of a corresponding record (e.g., in an external `lawyers` or
    `customers` table linked to the `user_id`).
5.  Active Persona Context: At runtime, the application must know the user's
    current operational context (e.g., `@ActivePersonaName = 'LAWYER'`). This
    context influences which persona-specific rules apply.

Permission Granting Layers (Checked in this order of priority):
----------------------------------------------------------------
The system checks these layers sequentially. The first layer that grants
permission allows the action (unless external factors like `cases.private` intervene).

LAYER 0: Direct User Permission Overrides (`user_permission_overrides`)
    - Purpose: Grants a specific permission directly to a `user_id`, bypassing
      roles and attributes. This is the highest priority check.
    - Persona-Aware: A grant can be generic (attribute_id = NULL, applies
      regardless of active persona) or specific to a persona (attribute_id is
      set, applies only when the user's active persona matches).
    - Check: Does a row exist for the `user_id`, `permission_id`, and
      matching `attribute_id` (or NULL)?

LAYER 1: Role/Attribute-Based Global Overrides (`permission_grants`)
    - Purpose: Grants broad permissions, often administrative (e.g.,
      'EDIT_ANY_CASE', 'VIEW_ANY_CASE'), based on the user's role and
      potentially their active persona. These typically bypass object-specific
      checks.
    - Persona-Aware: Defined by `required_role_id` and optionally
      `required_attribute_id`. The check verifies the user has the role and,
      if an attribute is required, that it matches the user's active persona
      AND the user is capable of acting as that persona.
    - Check: Does a rule exist in `permission_grants` for a known global
      override permission (like 'EDIT_ANY_CASE') that matches the user's role
      and active persona context?

LAYER 2: Case Ownership (Implicit Grant - `cases.creator_user_id`)
    - Purpose: The creator/owner of a case often has implicit full control
      over it.
    - Persona-Aware: Generally NOT persona-aware. Ownership grants access
      regardless of the user's current active persona.
    - Check: Is the `user_id` the `creator_user_id` listed on the specific `case`?

LAYER 3: Specific Case ACL Grant (`case_user_permissions`)
    - Purpose: Explicitly grants a specific user a specific permission on a
      specific case (Access Control List). This enables sharing/collaboration.
    - Persona-Aware: A grant can be generic (attribute_id = NULL) or specific
      to a persona (attribute_id is set).
    - Check: Does a row exist in `case_user_permissions` for the `case_id`,
      `user_id`, `permission_id`, and matching `attribute_id` (or NULL)?

LAYER 4: Role/Attribute-Based General Capabilities (`permission_grants`)
    - Purpose: Grants standard capabilities based on role and active persona
      (e.g., a USER acting as LAWYER can 'CREATE_CASE'). This is checked if
      no higher-priority layer granted access *and* the action is not specific
      to an existing case (or if layers 2 & 3 didn't apply).
    - Persona-Aware: Same mechanism as Layer 1, but checked for non-global
      permissions relevant to the action.
    - Check: Does a rule exist in `permission_grants` for the required
      permission that matches the user's role and active persona context?

External Factors: The `cases.private` Flag
--------------------------------------------
-   Purpose: Acts as an additional visibility check AFTER the main permission
    logic.
-   Interaction (Recommended Logic - Handle in Application Code):
    1. Perform the layered permission check (Layers 0-4 above).
    2. If Permission GRANTED (by any layer): Access is allowed, regardless
       of the `private` flag (owner, admin, specific grant bypass privacy).
    3. If Permission NOT GRANTED by Layers 0-4:
        a. Check `cases.private`. If TRUE (private): Access is DENIED.
        b. If FALSE (public): Optionally perform a *final* check using
           `permission_grants` for a general ""public access"" permission
           (e.g., 'VIEW_ANY_PUBLIC_CASE') based on the user's role/persona.
           Grant or deny based on this final check.

--------------------------------------------------------------------------------
|                              VALIDATION EXAMPLES                             |
--------------------------------------------------------------------------------

These examples illustrate the core logic. The actual implementation uses a
single, more complex `EXISTS` query combining these checks efficiently.

-- Parameters needed for checks:
-- :userId                 (INT) - ID of the user attempting the action
-- :permissionName         (TEXT)- Name of the permission required (e.g., 'EDIT_CASE_DETAILS')
-- :activePersonaName      (TEXT)- User's current persona ('LAWYER', 'CUSTOMER', etc.)
-- :caseId                 (INT) - ID of the specific case (if applicable)
-- :globalOverridePermName (TEXT)- Name of a related global override permission (e.g., 'EDIT_ANY_CASE')

-- Example 1: Checking a GENERAL capability (e.g., Can User 123 Create Cases as a Lawyer?)
-- Here, caseId is not relevant. We primarily check Layers 0 and 4.

-- Check Layer 0 (Direct User Override):

SELECT 1 FROM [user_permission_overrides] UPO
LEFT JOIN [user_attributes] UA 
	ON [UPO].[attribute_id] = [UA].[id]
JOIN [permissions] P 
	ON [UPO].[permission_id] = [P].[id]
WHERE 
	[UPO].[user_id] = :userId										   AND
	[P].[name]	    = 'CREATE_CASE'									   AND
	([UPO].[attribute_id] IS NULL OR [UA].[name] = :activePersonaName) AND
	([UPO].[attribute_id] IS NULL OR (:activePersonaName = 'LAWYER' AND EXISTS (SELECT 1 FROM [lawyers] WHERE [user_id] = :userId))); -- Safety check

-- If not found, Check Layer 4 (Role/Attribute Grant):

SELECT 1 FROM [permission_grants] PG
JOIN [user_roles]  UR 
	ON [PG].[required_role_id] = [UR].[role_id] AND 
	   [UR].[user_id]		   = :userId
JOIN [permissions] P  
	ON [PG].[permission_id] = [P].[id]
LEFT JOIN [user_attributes] UA 
	ON [PG].[required_attribute_id] = [UA].[id]
WHERE [P].[name] = 'CREATE_CASE'
  AND ([PG].[required_attribute_id] IS NULL OR [UA].[name] = :activePersonaName)
  AND ([PG].[required_attribute_id] IS NULL OR (:activePersonaName = 'LAWYER' AND EXISTS (SELECT 1 FROM [lawyers] WHERE [user_id] = :userId))); -- Safety check

-- Example 2: Checking a SPECIFIC case action (e.g., Can User 123 Edit Case 456 as a Customer?)
-- This requires checking all layers and potentially the 'private' flag.

-- Step A: Perform the combined SQL Permission Check (Layers 0, 1, 2, 3)
-- (Uses the complex EXISTS query developed previously, incorporating all layers)
-- Let's assume this query returns TRUE (hasPermission = true) or FALSE (hasPermission = false)

-- Step B: Application Logic incorporating the 'private' flag

-- Fetch case details, including the 'private' flag
SELECT private FROM cases WHERE id = :caseId;
-- Let's say result is isPrivate = TRUE/FALSE

-- Apply logic:
-- IF hasPermission THEN
--    Access ALLOWED (user has specific override, global override, ownership, or ACL grant)
-- ELSE
--    IF isPrivate THEN
--       Access DENIED (case is private and user lacks specific authorization)
--    ELSE
--       -- Case is public, user lacks specific authorization. Check general public access right.
--       -- Check Layer 4 for a permission like 'VIEW_ANY_PUBLIC_CASE' (or EDIT equivalent)
--       SELECT 1 FROM [permission_grants] [PG] ... WHERE [P].[name] = 'VIEW_ANY_PUBLIC_CASE' AND ... (matches role/persona)
--       -- IF public access right exists THEN
--       --    Access ALLOWED
--       -- ELSE
--       --    Access DENIED
--       -- ENDIF
--    ENDIF
-- ENDIF

-- NOTE: The single, optimized SQL query used in the C# `CheckUserCasePermissionAsync` function
-- combines the checks for layers 0, 1, 2, and 3 efficiently using `EXISTS` and `UNION ALL`.
-- The handling of the `cases.private` flag and the final public access check (if needed)
-- typically occurs in the surrounding application code.

*/

/* ========================================================================== */
/*                            DEFAULT DATA INSERTION                          */
/* ========================================================================== */

-- 1. Insert Default Roles (if they don't exist)

INSERT INTO [roles] ([name]) VALUES ('USER')  ON CONFLICT([name]) DO NOTHING;
INSERT INTO [roles] ([name]) VALUES ('ADMIN') ON CONFLICT([name]) DO NOTHING;

-- 2. Insert Default User Attributes (Personas - if they don't exist)

INSERT INTO [user_attributes] ([name]) VALUES ('LAWYER')   ON CONFLICT([name]) DO NOTHING;
INSERT INTO [user_attributes] ([name]) VALUES ('CUSTOMER') ON CONFLICT([name]) DO NOTHING;

-- Add others like 'MONITOR' if needed
-- INSERT INTO [user_attributes] ([name]) VALUES ('MONITOR') ON CONFLICT([name]) DO NOTHING;

-- 3. Insert Default Permissions (if they don't exist)

-- ORDINARY PERMISSIONS

INSERT INTO permissions ([name], [description]) VALUES ('CHAT_USER',	   'Allows chat user')							       ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('MANAGE_OWN_USER', 'Allows user manage configurations for their user') ON CONFLICT([name]) DO NOTHING;

-- CASE (SPECIFIC) PERMISSIONS

INSERT INTO permissions ([name], [description]) VALUES ('VIEW_OWN_CASES',		'Allows viewing cases associated with the user (e.g., as customer)') ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('VIEW_ANY_PUBLIC_CASE', 'Allows viewing any case marked as not private')					 ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('REGISTER_CASE',		'Allows register a new case')										 ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('ASSIGN_LAWYER_CASE',	'Allows editing the details of a specific case')					 ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('ASSIGN_CUSTOMER_CASE',	'Allows editing the details of a specific case')					 ON CONFLICT([name]) DO NOTHING;

-- ADMINISTRATOR PERMISSIONS

INSERT INTO [permissions] ([name], [description]) VALUES ('CHAT_ANY_USER',	 'Allows chat with any user in the system') ON CONFLICT([name]) DO NOTHING;
INSERT INTO [permissions] ([name], [description]) VALUES ('MANAGE_ANY_USER', 'Allows managing user accounts')			 ON CONFLICT([name]) DO NOTHING;

-- ADMINISTRATOR PERMISSIONS [CASE (SPECIFIC) PERMISSIONS]

INSERT INTO [permissions] ([name], [description]) VALUES ('VIEW_ANY_CASE',	   'Allows viewing any case in the system')			   ON CONFLICT([name]) DO NOTHING;
INSERT INTO [permissions] ([name], [description]) VALUES ('REGISTER_ANY_CASE', 'Allows editing details of any case in the system') ON CONFLICT([name]) DO NOTHING;
INSERT INTO [permissions] ([name], [description]) VALUES ('EDIT_ANY_CASE',	   'Allows editing details of any case in the system') ON CONFLICT([name]) DO NOTHING;

/*
								   [TABLE OF HIERARCHY]


							   | PERMISSION			    | OVERRIDE PERMISSION		  |
 	    [ORDINARY PERMISSIONS] |------------------------------------------------------|
							   | CHAT_USER				| CHAT_ANY_USER				  |
							   | MANAGE_OWN_USER		| MANAGE_ANY_USER			  |
 [CASE (SPECIFIC) PERMISSIONS] |------------------------------------------------------|
							   | VIEW_OWN_CASES			| VIEW_ANY_CASE				  |
							   | VIEW_ANY_PUBLIC_CASE	| VIEW_ANY_CASE				  |
							   | REGISTER_CASE			| REGISTER_ANY_CASE			  |
							   | ASSIGN_LAWYER_CASE		| EDIT_ANY_CASE				  |
							   | ASSIGN_CUSTOMER_CASE	| EDIT_ANY_CASE				  |

*/

-- 4. Insert Default Permission Grants (Rules)

-- Use subqueries to get IDs based on names for robustness.

-------------------------------------------------------------------------------------------------------
------------------------------------------------ USER -------------------------------------------------
-------------------------------------------------------------------------------------------------------

-----------------------------------------------------------------------------------------------------------
------------------------------------------------ CHAT_USER ------------------------------------------------
-----------------------------------------------------------------------------------------------------------

-- Grant: USER role -> CHAT_USER permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [user_attributes] [UA]
WHERE [P].[name]  = 'CHAT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

-----------------------------------------------------------------------------------------------------------------
------------------------------------------------ MANAGE_OWN_USER ------------------------------------------------
-----------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> MANAGE_OWN_USER permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [user_attributes] [UA]
WHERE [P].[name]  = 'MANAGE_OWN_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

----------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_OWN_CASES ------------------------------------------------
----------------------------------------------------------------------------------------------------------------

-- Grant: USER role + CUSTOMER attribute -> VIEW_OWN_CASES permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], [UA].[id]
FROM [permissions] [P], [roles] [R], [user_attributes] [UA]
WHERE
	[p].[name]  = 'VIEW_OWN_CASES' AND
	[r].[name]  = 'USER'		   AND
	[ua].[name] = 'CUSTOMER'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

-- Grant: USER role + LAWYER attribute -> VIEW_OWN_CASES permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], [UA].[id]
FROM [permissions] [P], [roles] [R], [user_attributes] [UA]
WHERE
	[p].[name]  = 'VIEW_OWN_CASES' AND
	[r].[name]  = 'USER'		   AND
	[ua].[name] = 'LAWYER'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

----------------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_ANY_PUBLIC_CASE ------------------------------------------------
----------------------------------------------------------------------------------------------------------------------

-- Grant: USER role + CUSTOMER attribute -> VIEW_ANY_PUBLIC_CASE permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], [UA].[id]
FROM [permissions] [P], [roles] [R], [user_attributes] [UA]
WHERE
	[p].[name]  = 'VIEW_ANY_PUBLIC_CASE' AND
	[r].[name]  = 'USER'				 AND
	[ua].[name] = 'CUSTOMER'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

-- Grant: USER role + LAWYER attribute -> VIEW_ANY_PUBLIC_CASE permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], [UA].[id]
FROM [permissions] [P], [roles] [R], [user_attributes] [UA]
WHERE
	[p].[name]  = 'VIEW_ANY_PUBLIC_CASE' AND
	[r].[name]  = 'USER'				 AND
	[ua].[name] = 'LAWYER'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;


---------------------------------------------------------------------------------------------------------------
------------------------------------------------ REGISTER_CASE ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: USER role + CUSTOMER attribute -> REGISTER_CASE permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], [UA].[id]
FROM [permissions] [P], [roles] [R], [user_attributes] [UA]
WHERE
	[p].[name]  = 'REGISTER_CASE' AND
	[r].[name]  = 'USER'	      AND
	[ua].[name] = 'CUSTOMER'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

-- Grant: USER role + LAWYER attribute -> REGISTER_CASE permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], [UA].[id]
FROM [permissions] [P], [roles] [R], [user_attributes] [UA]
WHERE
	[p].[name]  = 'REGISTER_CASE' AND
	[r].[name]  = 'USER'		  AND
	[ua].[name] = 'LAWYER'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

--------------------------------------------------------------------------------------------------------------------
------------------------------------------------ ASSIGN_LAWYER_CASE ------------------------------------------------
--------------------------------------------------------------------------------------------------------------------

-- Grant: USER role + CUSTOMER attribute -> ASSIGN_LAWYER_CASE permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], [UA].[id]
FROM [permissions] [P], [roles] [R], [user_attributes] [UA]
WHERE
	[p].[name]  = 'ASSIGN_LAWYER_CASE' AND
	[r].[name]  = 'USER'	           AND
	[ua].[name] = 'CUSTOMER'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

-- Grant: USER role + LAWYER attribute -> ASSIGN_LAWYER_CASE permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], [UA].[id]
FROM [permissions] [P], [roles] [R], [user_attributes] [UA]
WHERE
	[p].[name]  = 'ASSIGN_LAWYER_CASE' AND
	[r].[name]  = 'USER'		       AND
	[ua].[name] = 'LAWYER'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

----------------------------------------------------------------------------------------------------------------------
------------------------------------------------ ASSIGN_CUSTOMER_CASE ------------------------------------------------
----------------------------------------------------------------------------------------------------------------------

-- Grant: USER role + CUSTOMER attribute -> ASSIGN_CUSTOMER_CASE permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], [UA].[id]
FROM [permissions] [P], [roles] [R], [user_attributes] [UA]
WHERE
	[p].[name]  = 'ASSIGN_CUSTOMER_CASE' AND
	[r].[name]  = 'USER'	             AND
	[ua].[name] = 'CUSTOMER'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

-- Grant: USER role + LAWYER attribute -> ASSIGN_CUSTOMER_CASE permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], [UA].[id]
FROM [permissions] [P], [roles] [R], [user_attributes] [UA]
WHERE
	[p].[name]  = 'ASSIGN_CUSTOMER_CASE' AND
	[r].[name]  = 'USER'		         AND
	[ua].[name] = 'LAWYER'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;


-------------------------------------------------------------------------------------------------------
------------------------------------------------ ADMIN ------------------------------------------------
-------------------------------------------------------------------------------------------------------

---------------------------------------------------------------------------------------------------------------
------------------------------------------------ CHAT_ANY_USER ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> CHAT_ANY_USER permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'CHAT_ANY_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_ANY_CASE ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> VIEW_ANY_CASE permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'VIEW_ANY_CASE' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------
------------------------------------------------ EDIT_ANY_CASE ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> EDIT_ANY_CASE permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'EDIT_ANY_CASE' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

-------------------------------------------------------------------------------------------------------------------
------------------------------------------------ REGISTER_ANY_CASE ------------------------------------------------
-------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> REGISTER_ANY_CASE permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'REGISTER_ANY_CASE' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;

-----------------------------------------------------------------------------------------------------------------
------------------------------------------------ MANAGE_ANY_USER ------------------------------------------------
-----------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> MANAGE_ANY_USER permission

INSERT INTO [permission_grants] ([permission_id], [required_role_id], [required_attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'MANAGE_ANY_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [required_role_id], [required_attribute_id]) DO NOTHING;


/* ========================================================================== */
/*                     END OF PERMISSION ARCHITECTURE                         */
/* ========================================================================== */

COMMIT;
");

		return resultContructor.Build();
    }
}
