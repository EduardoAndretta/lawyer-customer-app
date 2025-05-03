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
	private				 BOOLEAN NOT NULL DEFAULT 1,
	CONSTRAINT unq_users_email UNIQUE ( email )
 );

CREATE TABLE IF NOT EXISTS blocked_users ( 
	id					INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	user_id				INTEGER NOT NULL,

	reason				TEXT NOT NULL,
	date				TEXT DEFAULT CURRENT_TIMESTAMP,

	CONSTRAINT unq_users_block UNIQUE ( user_id ),
	FOREIGN KEY ( user_id ) REFERENCES users( id )  
 );

CREATE TABLE IF NOT EXISTS blocked_lawyers ( 
	user_id				INTEGER NOT NULL,
	lawyer_id			INTEGER NOT NULL,

	reason				TEXT NOT NULL,
	date				TEXT DEFAULT CURRENT_TIMESTAMP,

	PRIMARY KEY ( user_id, lawyer_id ),
	FOREIGN KEY ( user_id )	  REFERENCES users( id ),  
	FOREIGN KEY ( lawyer_id ) REFERENCES lawyers( id )  
);

CREATE TABLE IF NOT EXISTS blocked_customers ( 
	user_id				INTEGER NOT NULL,
	customer_id			INTEGER NOT NULL,

	reason				TEXT NOT NULL,
	date				TEXT DEFAULT CURRENT_TIMESTAMP,

	PRIMARY KEY ( user_id, lawyer_id ),
	FOREIGN KEY ( user_id )	    REFERENCES users( id ),  
	FOREIGN KEY ( customer_id ) REFERENCES customers( id )  
);

CREATE TABLE IF NOT EXISTS address_users ( 
	user_id				INTEGER NOT NULL,
	
	zip_code	 TEXT,
	house_number TEXT,
	complement   TEXT,
	district	 TEXT,
	city		 TEXT,
	state		 TEXT,
	country		 TEXT,
	
	PRIMARY KEY ( user_id ),
	FOREIGN KEY ( user_id ) REFERENCES users( id )
 );

CREATE TABLE IF NOT EXISTS address_attributes ( 
	user_id			INTEGER NOT NULL,
	lawyer_id		INTEGER NOT NULL,

	zip_code	 TEXT,
	house_number TEXT,
	complement   TEXT,
	district	 TEXT,
	city		 TEXT,
	state		 TEXT,
	country		 TEXT,

	PRIMARY KEY ( user_id, lawyer_id ),
	FOREIGN KEY ( user_id )		 REFERENCES users( id ),  
	FOREIGN KEY ( attribute_id ) REFERENCES attributes( id )  
);

CREATE TABLE IF NOT EXISTS address_customers ( 
	user_id			INTEGER NOT NULL,
	customer_id		INTEGER NOT NULL,

	zip_code	 TEXT,
	house_number TEXT,
	complement   TEXT,
	district	 TEXT,
	city		 TEXT,
	state		 TEXT,
	country		 TEXT,

	PRIMARY KEY ( user_id, customer_id ),
	FOREIGN KEY ( user_id )		 REFERENCES users( id ),  
	FOREIGN KEY ( attribute_id ) REFERENCES attributes( id )  
);


CREATE TABLE IF NOT EXISTS documents_users ( 
	user_id				 INTEGER NOT NULL,
	
	type				 TEXT,
	identifier_document	 TEXT,
	
	PRIMARY KEY ( user_id ),
	FOREIGN KEY ( user_id )		 REFERENCES users( id ), 
	CHECK ( type IN ('PF', 'PJ') )
 );

CREATE TABLE IF NOT EXISTS documents_lawyers ( 
	user_id				INTEGER NOT NULL,
	lawyer_id			INTEGER NOT NULL,

	type				 TEXT,
	identifier_document	 TEXT,

	PRIMARY KEY ( user_id, lawyer_id ),
	FOREIGN KEY ( user_id )	  REFERENCES users( id ),  
	FOREIGN KEY ( lawyer_id ) REFERENCES lawyers( id ),
	CHECK ( type IN ('PF', 'PJ') )
);

CREATE TABLE IF NOT EXISTS documents_customers ( 
	user_id				INTEGER NOT NULL,
	customer_id			INTEGER NOT NULL,

	type				 TEXT,
	identifier_document	 TEXT,

	PRIMARY KEY ( user_id, customer_id ),
	FOREIGN KEY ( user_id )	    REFERENCES users( id ),  
	FOREIGN KEY ( customer_id ) REFERENCES customers( id ),
	CHECK ( type IN ('PF', 'PJ') )
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

CREATE TABLE IF NOT EXISTS attributes (
    id              INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    name            TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS permissions (
    id              INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    name            TEXT NOT NULL UNIQUE,
    description     TEXT
);

CREATE TABLE IF NOT EXISTS roles (
    user_id         INTEGER NOT NULL,
    role_id         INTEGER NOT NULL,
    PRIMARY KEY (user_id, role_id),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS permission_grants (
    id                      INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    permission_id           INTEGER NOT NULL,
    role_id					INTEGER NOT NULL,
    attribute_id			INTEGER NULL,

    FOREIGN KEY (permission_id) REFERENCES permissions(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id)		REFERENCES roles(id)       ON DELETE CASCADE,
    FOREIGN KEY (attribute_id)	REFERENCES attributes(id)  ON DELETE CASCADE,

    UNIQUE (permission_id, role_id, attribute_id)
);

CREATE TABLE IF NOT EXISTS permission_grants_relationship (
    related_user_id INTEGER NOT NULL,
    permission_id   INTEGER NOT NULL,
	role_id         INTEGER NOT NULL,
    user_id         INTEGER NOT NULL,
    attribute_id    INTEGER NULL,

    PRIMARY KEY (related_user_id, role_id, user_id, permission_id, attribute_id),

    FOREIGN KEY (related_user_id) REFERENCES users(id)       ON DELETE CASCADE,
    FOREIGN KEY (permission_id)   REFERENCES permissions(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id)         REFERENCES roles(id)       ON DELETE CASCADE,
    FOREIGN KEY (user_id)         REFERENCES users(id)       ON DELETE CASCADE,
    FOREIGN KEY (attribute_id)    REFERENCES attributes(id)  ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS permission_grants_case (
    related_case_id INTEGER NOT NULL,
    permission_id   INTEGER NOT NULL,
	role_id         INTEGER NOT NULL,
    user_id         INTEGER NOT NULL,
    attribute_id    INTEGER NULL,

    PRIMARY KEY (related_case_id, role_id, user_id, permission_id, attribute_id),

    FOREIGN KEY (related_case_id) REFERENCES cases(id)       ON DELETE CASCADE,
    FOREIGN KEY (permission_id)   REFERENCES permissions(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id)         REFERENCES roles(id)       ON DELETE CASCADE,
    FOREIGN KEY (user_id)         REFERENCES users(id)       ON DELETE CASCADE,
    FOREIGN KEY (attribute_id)    REFERENCES attributes(id)  ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS permission_grants_user (
	user_id         INTEGER NOT NULL,
	permission_id   INTEGER NOT NULL,
	role_id         INTEGER NOT NULL,
	attribute_id    INTEGER NULL,

	PRIMARY KEY (user_id, permission_id, role_id, attribute_id),

	FOREIGN KEY (permission_id) REFERENCES permissions(id) ON DELETE CASCADE,
	FOREIGN KEY (role_id)		REFERENCES roles(id)	   ON DELETE CASCADE,
	FOREIGN KEY (user_id)       REFERENCES users(id)       ON DELETE CASCADE,
	FOREIGN KEY (attribute_id)  REFERENCES attributes(id)  ON DELETE CASCADE
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

This system employs a multi-layered, context-aware approach to determine user
permissions. Authorization depends on the user's identity, assigned roles,
active persona (Attribute), the specific action (Permission), potentially the
target data object (e.g., a Case), and object properties (e.g., Case privacy).

Core Concepts:
---------------
1.  Users: Standard user accounts (`users` table - assumed external).
2.  Roles: High-level categories assigned to users (e.g., 'USER', 'ADMIN').
    Managed in `roles` and assigned via `roles`.
3.  Permissions: Granular actions defined in the system (e.g., 'CREATE_CASE',
    'EDIT_CASE_DETAILS'). Defined in `permissions`.
4.  Attributes (Personas): Specific capacities or contexts a user operates
    within (e.g., 'LAWYER', 'CUSTOMER'). Defined in `attributes`.
    A user's *capability* to act under a specific persona is determined by
    external factors (e.g., existence of a linked record in `lawyers` or
    `customers` tables). The permission check query validates this capability.
5.  Active Persona Context: The application must provide the user's currently
    active persona name (e.g., 'LAWYER') during permission checks. This context
    is crucial for evaluating persona-specific rules.

Permission Granting Tables:
--------------------------
*   `permission_grants_user`: Direct permission grants to a specific User,
    requiring a specific Role, and optionally specific to an active Persona.
    These override role-based permissions for general actions.
*   `permission_grants`: Defines general permissions based on Role and
    optionally a required Persona (Attribute). These are the standard rules.
*   `permission_grants_case`: Grants a specific Usera and a specific Role a specific Permission on a
    specific Case, optionally specific to an active Persona (Access Control List - ACL).

Permission Check Logic & Priority Order:
----------------------------------------
The system determines if the action is GENERAL (not specific to a case) or
CASE-SPECIFIC based on whether a valid `caseId` is provided.

*** A) For GENERAL Actions (e.g., Create Case, View Dashboard): ***

    LAYER 1: User-Specific Grants (`permission_grants_user`)
        - Purpose: Highest priority check for general actions. Allows direct
          assignment of permissions to users, overriding standard role grants.
        - Persona-Aware: Yes. Grant requires the user to have the specified
          `role_id` and checks against the active persona if `attribute_id`
          is set (NULL means persona-agnostic). Also verifies persona capability.
        - Check: Does a matching row exist for `user_id`, `role_id`,
          `permission_id`, and `attribute_id` (considering active persona)?

    LAYER 2: Role/Attribute-Based Grants (`permission_grants`)
        - Purpose: Standard mechanism for granting permissions based on roles
          and active persona. Checked if Layer 1 grants no permission.
        - Persona-Aware: Yes. Checks if a rule exists matching the user's role(s),
          the required permission, and the active persona (if `required_attribute_id`
          is set). Also verifies persona capability.
        - Check: Does a matching rule exist for `required_role_id`,
          `permission_id`, and `required_attribute_id` (considering active persona)?

*** B) For CASE-SPECIFIC Actions (e.g., Edit Case, View Case Details): ***

    LAYER 1: Case-Specific ACL Grant (`permission_grants_case`)
        - Purpose: Highest priority for case actions. Allows explicit sharing
          and fine-grained control per case.
        - Persona-Aware: Yes. Grant can be persona-agnostic (`attribute_id` is NULL)
          or specific to the active persona (`attribute_id` is set). Verifies
          persona capability if attribute_id is set.
        - Check: Does a matching row exist for `related_case_id`, `user_id`,
          `permission_id`, and `attribute_id` (considering active persona)?

    LAYER 2: Case Ownership (`cases.creator_user_id`)
        - Purpose: Implicit grant, usually providing full control to the creator.
        - Persona-Aware: No. Ownership applies regardless of active persona.
        - Check: Is the `user_id` the `creator_user_id` for the `case_id`?

    LAYER 3: User-Specific Grants (`permission_grants_user`)
        - Purpose: Checks if the user has a direct, general override for the
          required permission that might apply to any case.
        - Persona-Aware: Yes. Same logic as Layer 1 for General Actions.
        - Check: Does a matching row exist for `user_id`, `role_id`,
          `permission_id`, and `attribute_id` (considering active persona)?

    LAYER 4: Role/Attribute-Based Grants (`permission_grants`)
        - Purpose: Checks standard role/attribute rules for either a specific
          *global override* permission (e.g., 'EDIT_ANY_CASE') OR the specific
          *required permission* (e.g., 'EDIT_CASE_DETAILS').
        - Persona-Aware: Yes. Same logic as Layer 2 for General Actions, applied
          first for a potential global override permission, then potentially
          for the specific required permission. Verifies persona capability.
        - Check: Does a matching rule exist for a relevant global override OR the
          specific permission, considering `required_role_id` and
          `required_attribute_id` against the active persona?

External Factors: The `cases.private` Flag
--------------------------------------------
-   Purpose: Provides an object-level visibility control, checked *after* the
    main permission logic.
-   Interaction (Recommended Logic - Handle in Application Code):
    1. Execute the appropriate layered permission check (A or B above). Let the
       result be `hasPermission`.
    2. IF `hasPermission` THEN: Access is ALLOWED (privacy is bypassed by
       explicit grants, ownership, or overrides).
    3. IF NOT `hasPermission` THEN:
        a. Fetch `cases.private` status for the `caseId`. Let it be `isPrivate`.
        b. IF `isPrivate` THEN: Access is DENIED.
        c. IF NOT `isPrivate` (case is public) THEN: Optionally, perform a final
           check (using General Action logic, Layers 1 & 2) for a generic
           ""public access"" permission (e.g., 'VIEW_ANY_PUBLIC_CASE'). Grant
           or deny based on this final check.

--------------------------------------------------------------------------------
|                              VALIDATION EXAMPLES                             |
--------------------------------------------------------------------------------

These examples illustrate the logic for individual layers. The actual C#
implementation uses a single, optimized SQL query (`CheckPermissionAsync`) that
dynamically constructs the appropriate `UNION ALL` sequence based on context
(general vs. case-specific) and efficiently checks all relevant layers.

-- Parameters needed for checks:
-- :userId                   (INT) - ID of the user attempting the action
-- :requiredPermissionId     (INT) - ID of the permission required
-- :activePersonaName        (TEXT)- User's current persona ('LAWYER', 'CUSTOMER', etc.)
-- :caseId                   (INT) - ID of the specific case (0 or less for general actions)
-- :globalOverridePermName   (TEXT)- Name of a related global override permission (for case actions)

-- Example 1: Illustrative Check for GENERAL Action (e.g., Create Case as Lawyer)

-- Check Layer 1 (`permission_grants_user`):
SELECT 1 FROM [permission_grants_user] [PGU]
JOIN [roles] [UR_PGU] ON [PGU].[user_id] = [UR_PGU].[user_id] AND [PGU].[role_id] = [UR_PGU].[role_id]
LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
WHERE [PGU].[user_id]		= :userId
  AND [PGU].[permission_id] = :requiredPermissionId -- ID for 'CREATE_CASE'
  AND ([PGU].[attribute_id] IS NULL OR [A_PGU].[name] = :activePersonaName) -- Matches 'LAWYER'
  -- AND (Generalized Capability Check SQL)

-- If not found, Check Layer 2 (`permission_grants`):
SELECT 1 FROM [permission_grants] [PG]
JOIN [roles] [UR] ON [PG].[required_role_id] = [UR].[role_id] AND [UR].[user_id] = :userId
LEFT JOIN [attributes] [A] ON [PG].[required_attribute_id] = [A].[id]
WHERE [PG].[permission_id] = :requiredPermissionId -- ID for 'CREATE_CASE'
  AND ([PG].[required_attribute_id] IS NULL OR [A].[name] = :activePersonaName) -- Matches 'LAWYER'
  -- AND (Generalized Capability Check SQL)


-- Example 2: Illustrative Check for CASE-SPECIFIC Action (e.g., Edit Case 456 as Customer)

-- Check Layer 1 (`permission_grants_case`):
SELECT 1 FROM [permission_grants_case] [PGC]
LEFT JOIN [attributes] [A_PGC] ON [PGC].[attribute_id] = [A_PGC].[id]
WHERE [PGC].[related_case_id] = :caseId -- 456
  AND [PGC].[user_id]		  = :userId
  AND [PGC].[permission_id]	  = :requiredPermissionId -- ID for 'EDIT_CASE_DETAILS'
  AND ([PGC].[attribute_id] IS NULL OR [A_PGC].[name] = :activePersonaName) -- Matches 'CUSTOMER'
  -- AND (Generalized Capability Check SQL)

-- If not found, Check Layer 2 (Ownership):
SELECT 1 FROM [cases] [C] WHERE [C].[id] = :caseId AND [C].[creator_user_id] = :userId;

-- If not found, Check Layer 3 (`permission_grants_user`):
SELECT 1 FROM [permission_grants_user] [PGU]
-- ... (similar query as in Example 1, Layer 1) ...

-- If not found, Check Layer 4 (`permission_grants` - first for global override, then specific):
SELECT 1 FROM [permission_grants] [PG_G]
JOIN [permissions] [P_G] ON [PG_G].[permission_id] = [P_G].[id]
-- ... (rest of query joining roles, attributes) ...
WHERE [P_G].[name] = :globalOverridePermName -- e.g., 'EDIT_ANY_CASE'
  -- AND (Persona/Capability Checks) ...
-- UNION ALL (potentially check specific permission if global failed) ...

-- Application logic then handles the result and the 'cases.private' flag as described above.

*/
/* ========================================================================== */
/*                            DEFAULT DATA INSERTION                          */
/* ========================================================================== */

-- 1. [Insert Default Roles (if they don't exist)]

INSERT INTO [roles] ([name]) VALUES ('USER')  ON CONFLICT([name]) DO NOTHING;
INSERT INTO [roles] ([name]) VALUES ('ADMIN') ON CONFLICT([name]) DO NOTHING;

-- 2. [Insert Default User Attributes (Personas - if they don't exist)]

INSERT INTO [attributes] ([name]) VALUES ('LAWYER')   ON CONFLICT([name]) DO NOTHING;
INSERT INTO [attributes] ([name]) VALUES ('CUSTOMER') ON CONFLICT([name]) DO NOTHING;

-- Add others like 'MONITOR' if needed
-- INSERT INTO [attributes] ([name]) VALUES ('MONITOR') ON CONFLICT([name]) DO NOTHING;

-- 3. [Insert Default Permissions (if they don't exist)]

-- /// <summary>
-- /// CASE
-- /// </summary>

-- // =================== [   ACL   ] =================== //

INSERT INTO permissions ([name], [description]) VALUES ('VIEW_CASE',					'Allows viewing cases')							  ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('ASSIGN_LAWYER_CASE',			'Allows editing the lawyer of a specific case')	  ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('ASSIGN_CUSTOMER_CASE',			'Allows editing the customer of a specific case') ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('GRANT_PERMISSIONS_CASE',		'Allows grant permissions to a specific case')	  ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('REVOKE_PERMISSIONS_CASE',		'Allows revoke permissions to a specific case')	  ON CONFLICT([name]) DO NOTHING;

-- // =================== [ NOT ACL ] =================== //

INSERT INTO permissions ([name], [description]) VALUES ('REGISTER_CASE',			   'Allows register a new case')										ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('VIEW_OWN_CASE',			   'Allows viewing cases associated with the user (e.g., as customer)') ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('VIEW_PUBLIC_CASE',			   'Allows viewing case marked as not private')						    ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('ASSIGN_LAWYER_OWN_CASE',	   'Allows editing the lawyer of a own case')						    ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('ASSIGN_CUSTOMER_OWN_CASE',	   'Allows editing the customer of a own case')						    ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('GRANT_PERMISSIONS_OWN_CASE',  'Allows grant permissions to a own case')						    ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('REVOKE_PERMISSIONS_OWN_CASE', 'Allows revoke permissions to a own case')						    ON CONFLICT([name]) DO NOTHING;

-- /// <summary>
-- /// RELATIONSHIP
-- /// </summary>

-- // =================== [   ACL   ] =================== //

INSERT INTO permissions ([name], [description]) VALUES ('GRANT_PERMISSIONS_CUSTOMER_ACCOUNT_USER',  'Allows grant permission for user with customer account')  ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('REVOKE_PERMISSIONS_CUSTOMER_ACCOUNT_USER', 'Allows revoke permission for user with customer account') ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER',  'Allows grant permission for user with lawyer account')  ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('REVOKE_PERMISSIONS_LAWYER_ACCOUNT_USER', 'Allows revoke permission for user with lawyer account') ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('GRANT_PERMISSIONS_USER',  'Allows grant permission for user')  ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('REVOKE_PERMISSIONS_USER', 'Allows revoke permission for user') ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('VIEW_USER',		          'Allows view user')				   ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('VIEW_LAWYER_ACCOUNT_USER',	  'Allows view user lawyer account')   ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('VIEW_CUSTOMER_ACCOUNT_USER', 'Allows view user customer account') ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('EDIT_USER',			      'Allows user edit values and configurations for their user')			   ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('EDIT_LAWYER_ACCOUNT_USER',   'Allows user edit values and configurations for their lawyer account')   ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('EDIT_CUSTOMER_ACCOUNT_USER', 'Allows user edit values and configurations for their customer account') ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('CHAT_USER', 'Allows chat user') ON CONFLICT([name]) DO NOTHING;

-- // =================== [ NOT ACL ] =================== //

INSERT INTO permissions ([name], [description]) VALUES ('REGISTER_USER','Allows register an user') ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('EDIT_OWN_USER',				  'Allows user edit values and configurations for their user') ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('EDIT_OWN_LAWYER_ACCOUNT_USER',	  'Allows user edit values and configurations for their lawyer account.')   ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('EDIT_OWN_CUSTOMER_ACCOUNT_USER', 'Allows user edit values and configurations for their customer account.') ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('VIEW_OWN_USER',	'Allows own view public user')  ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('VIEW_PUBLIC_USER', 'Allows user view public user') ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('VIEW_OWN_LAWYER_ACCOUNT_USER',      'Allows user view own lawyer account.')      ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('VIEW_OWN_CUSTOMER_ACCOUNT_USER',    'Allows user view own customer account.')    ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('VIEW_PUBLIC_LAWYER_ACCOUNT_USER',   'Allows user view public customer account.') ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER', 'Allows user view public customer account.') ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('GRANT_PERMISSIONS_OWN_USER',				   'Allows grant permission for own user.')				ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER',   'Allows grant permission for own lawyer account.')	ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER', 'Allows grant permission for own customer account.') ON CONFLICT([name]) DO NOTHING;

INSERT INTO permissions ([name], [description]) VALUES ('REVOKE_PERMISSIONS_OWN_USER',					'Allows revoke permission for own user.')			  ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER',	'Allows revoke permission for own lawyer account.')   ON CONFLICT([name]) DO NOTHING;
INSERT INTO permissions ([name], [description]) VALUES ('REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER', 'Allows revoke permission for own customer account.') ON CONFLICT([name]) DO NOTHING;

-- [ADMINISTRATOR PERMISSIONS]

-- /// <summary>
-- /// CASE
-- /// </summary>

-- // =================== [   ACL   ] =================== //

INSERT INTO [permissions] ([name], [description]) VALUES ('VIEW_ANY_CASE',			     'Allows viewing any case in the system')			    ON CONFLICT([name]) DO NOTHING;

INSERT INTO [permissions] ([name], [description]) VALUES ('ASSIGN_LAWYER_ANY_CASE',	     'Allows editing lawyer of any case in the system')     ON CONFLICT([name]) DO NOTHING;
INSERT INTO [permissions] ([name], [description]) VALUES ('ASSIGN_CUSTOMER_ANY_CASE',    'Allows editing customer of any case in the system')   ON CONFLICT([name]) DO NOTHING;

INSERT INTO [permissions] ([name], [description]) VALUES ('GRANT_PERMISSIONS_ANY_CASE',  'Allows grant permissions to any case in the system')  ON CONFLICT([name]) DO NOTHING;
INSERT INTO [permissions] ([name], [description]) VALUES ('REVOKE_PERMISSIONS_ANY_CASE', 'Allows revoke permissions to any case in the system') ON CONFLICT([name]) DO NOTHING;

-- /// <summary>
-- /// RELATIONSHIP
-- /// </summary>
 
-- // =================== [   ACL   ] =================== //

INSERT INTO [permissions] ([name], [description]) VALUES ('VIEW_ANY_LAWYER_ACCOUNT_USER',   'Allows view any lawyer account of any user')   ON CONFLICT([name]) DO NOTHING;
INSERT INTO [permissions] ([name], [description]) VALUES ('VIEW_ANY_CUSTOMER_ACCOUNT_USER', 'Allows view any customer account of any user') ON CONFLICT([name]) DO NOTHING;
INSERT INTO [permissions] ([name], [description]) VALUES ('VIEW_ANY_USER',					'Allows view any user')						    ON CONFLICT([name]) DO NOTHING;

INSERT INTO [permissions] ([name], [description]) VALUES ('GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER',	'Allows grant permission for any user with lawyer account')   ON CONFLICT([name]) DO NOTHING;
INSERT INTO [permissions] ([name], [description]) VALUES ('GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER',	'Allows grant permission for any user with customer account') ON CONFLICT([name]) DO NOTHING;


INSERT INTO [permissions] ([name], [description]) VALUES ('REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER',	 'Allows revoke permission for any user with lawyer account')   ON CONFLICT([name]) DO NOTHING;
INSERT INTO [permissions] ([name], [description]) VALUES ('REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER', 'Allows revoke permission for any user with customer account') ON CONFLICT([name]) DO NOTHING;


INSERT INTO [permissions] ([name], [description]) VALUES ('GRANT_PERMISSIONS_ANY_USER',  'Allows grant permission for any user')  ON CONFLICT([name]) DO NOTHING;
INSERT INTO [permissions] ([name], [description]) VALUES ('REVOKE_PERMISSIONS_ANY_USER', 'Allows revoke permission for any user') ON CONFLICT([name]) DO NOTHING;

INSERT INTO [permissions] ([name], [description]) VALUES ('EDIT_ANY_USER', 'Allows edit values and configurations of any user accounts') ON CONFLICT([name]) DO NOTHING;

INSERT INTO [permissions] ([name], [description]) VALUES ('EDIT_ANY_LAWYER_ACCOUNT_USER',  'Allows edit values and configurations of any lawyer account')   ON CONFLICT([name]) DO NOTHING;
INSERT INTO [permissions] ([name], [description]) VALUES ('EDIT_ANY_LAWYER_CUSTOMER_USER', 'Allows edit values and configurations of any customer account') ON CONFLICT([name]) DO NOTHING;


INSERT INTO [permissions] ([name], [description]) VALUES ('CHAT_ANY_USER', 'Allows chat with any user in the system') ON CONFLICT([name]) DO NOTHING;

       /*

     [TABLE OF HIERARCHY]

                                      | PERMISSION											| OVERRIDE PERMISSION													|
 [RELATIONSHIP (NOT ACL) PERMISSIONS] |-----------------------------------------------------------------------------------------------------------------------------|
                                      | GRANT_PERMISSIONS_OWN_USER							| GRANT_PERMISSIONS_ANY_USER											|
                                      | GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER			| GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER								|
                                      | GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER			| GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER							|
                                      | REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER			| REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER							|
                                      | REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER		| REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER                          |
                                      | REVOKE_PERMISSIONS_OWN_USER							| REVOKE_PERMISSIONS_ANY_USER											|
                                      | REGISTER_USER										| REGISTER_USER													     	|
                                      | EDIT_OWN_USER										| EDIT_ANY_USER											     			|
                                      | EDIT_OWN_LAWYER_ACCOUNT_USER						| EDIT_ANY_LAWYER_ACCOUNT_USER											|
                                      | EDIT_OWN_CUSTOMER_ACCOUNT_USER						| EDIT_ANY_CUSTOMER_ACCOUNT_USER										|
                                      | VIEW_OWN_USER										| VIEW_ANY_USER															|
                                      | VIEW_PUBLIC_USER									| VIEW_ANY_USER															|
                                      | VIEW_PUBLIC_LAWYER_ACCOUNT_USER						| VIEW_ANY_LAWYER_ACCOUNT_USER											|
                                      | VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER					| VIEW_ANY_CUSTOMER_ACCOUNT_USER										|
                                      | VIEW_OWN_LAWYER_ACCOUNT_USER					    | VIEW_ANY_LAWYER_ACCOUNT_USER										    |
                                      | VIEW_OWN_CUSTOMER_ACCOUNT_USER					    | VIEW_ANY_CUSTOMER_ACCOUNT_USER										|
     [RELATIONSHIP (ACL) PERMISSIONS] |-----------------------------------------------------------------------------------------------------------------------------|
                                      | GRANT_PERMISSIONS_USER								| GRANT_PERMISSIONS_ANY_USER											|
                                      | GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER		        | GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER								|
    								  | GRANT_PERMISSIONS_CUSTOMER_ACCOUNT_USER		        | GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER							|
                                      | REVOKE_PERMISSIONS_USER								| REVOKE_PERMISSIONS_ANY_USER											|  
								      | REVOKE_PERMISSIONS_CUSTOMER_ACCOUNT_USER		    | REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER							|
                                      | REVOKE_PERMISSIONS_LAWYER_ACCOUNT_USER		        | REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER							|
                                      | CHAT_USER											| CHAT_ANY_USER															|
                                      | VIEW_USER											| VIEW_ANY_USER															|
                                      | VIEW_LAWYER_ACCOUNT_USER							| VIEW_ANY_LAWYER_ACCOUNT_USER											|
                                      | VIEW_CUSTOMER_ACCOUNT_USER							| VIEW_ANY_CUSTOMER_ACCOUNT_USER										|
                                      | EDIT_USER											| EDIT_ANY_USER											     			|
                                      | EDIT_LAWYER_ACCOUNT_USER							| EDIT_ANY_LAWYER_ACCOUNT_USER											|
                                      | EDIT_CUSTOMER_ACCOUNT_USER							| EDIT_ANY_CUSTOMER_ACCOUNT_USER										|
         [CASE (NOT ACL) PERMISSIONS] |-----------------------------------------------------------------------------------------------------------------------------|
                                      | REGISTER_CASE										| REGISTER_CASE															|
                                      | VIEW_OWN_CASE										| VIEW_ANY_CASE															|
                                      | VIEW_PUBLIC_CASE									| VIEW_PUBLIC_CASE														|
                                      | ASSIGN_LAWYER_OWN_CASE								| ASSIGN_LAWYER_ANY_CASE												|
                                      | ASSIGN_CUSTOMER_OWN_CASE							| ASSIGN_CUSTOMER_ANY_CASE												|
                                      | GRANT_PERMISSIONS_OWN_CASE							| GRANT_PERMISSIONS_ANY_CASE											|
                                      | REVOKE_PERMISSIONS_OWN_CASE							| REVOKE_PERMISSIONS_ANY_CASE											|
             [CASE (ACL) PERMISSIONS] |-----------------------------------------------------------------------------------------------------------------------------|
                                      | VIEW_CASE											| VIEW_ANY_CASE															|
                                      | ASSIGN_LAWYER_CASE									| ASSIGN_LAWYER_ANY_CASE												|
                                      | ASSIGN_CUSTOMER_CASE								| ASSIGN_CUSTOMER_ANY_CASE												|
                                      | GRANT_PERMISSIONS_CASE								| GRANT_PERMISSIONS_ANY_CASE											|
                                      | REVOKE_PERMISSIONS_CASE								| REVOKE_PERMISSIONS_ANY_CASE											|
   */

-- 4. [Insert Default Permission Grants (Rules)]

-------------------------------------------------------------------------------------------------------
------------------------------------------------ USER -------------------------------------------------
-------------------------------------------------------------------------------------------------------

---------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ GRANT_PERMISSIONS_OWN_USER ------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> GRANT_PERMISSIONS_OWN_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'GRANT_PERMISSIONS_OWN_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-------------------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER ------------------------------------------------
-------------------------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER ------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

----------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ REVOKE_PERMISSIONS_OWN_USER ------------------------------------------------
----------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> REVOKE_PERMISSIONS_OWN_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'REVOKE_PERMISSIONS_OWN_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

----------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER ------------------------------------------------
----------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

----------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER ------------------------------------------------
----------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-----------------------------------------------------------------------------------------------------------------
------------------------------------------------ EDIT_OWN_USER ------------------------------------------------
-----------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> EDIT_OWN_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'EDIT_OWN_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ EDIT_OWN_LAWYER_ACCOUNT_USER ------------------------------------------------
------------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> EDIT_OWN_LAWYER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'EDIT_OWN_LAWYER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

--------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ EDIT_OWN_CUSTOMER_ACCOUNT_USER ------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> EDIT_OWN_CUSTOMER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'EDIT_OWN_CUSTOMER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_OWN_USER ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> VIEW_OWN_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'VIEW_OWN_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_OWN_LAWYER_ACCOUNT_USER ------------------------------------------------
------------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> VIEW_OWN_LAWYER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'VIEW_OWN_LAWYER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;


--------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_OWN_CUSTOMER_ACCOUNT_USER ------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> VIEW_OWN_CUSTOMER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'VIEW_OWN_CUSTOMER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_PUBLIC_USER ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> VIEW_PUBLIC_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'VIEW_PUBLIC_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_PUBLIC_LAWYER_ACCOUNT_USER ------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> VIEW_PUBLIC_LAWYER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'VIEW_PUBLIC_LAWYER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-----------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER ------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_OWN_CASE ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: USER role + CUSTOMER attribute -> VIEW_OWN_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'VIEW_OWN_CASE' AND
	[R].[name] = 'USER'		     AND
	[A].[name] = 'CUSTOMER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-- Grant: USER role + LAWYER attribute -> VIEW_OWN_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'VIEW_OWN_CASE' AND
	[R].[name] = 'USER'		     AND
	[A].[name] = 'LAWYER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

------------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_PUBLIC_CASE ------------------------------------------------
------------------------------------------------------------------------------------------------------------------

-- Grant: USER role + CUSTOMER attribute -> VIEW_PUBLIC_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'VIEW_PUBLIC_CASE' AND
	[R].[name] = 'USER'				AND
	[A].[name] = 'CUSTOMER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-- Grant: USER role + LAWYER attribute -> VIEW_PUBLIC_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'VIEW_PUBLIC_CASE' AND
	[R].[name] = 'USER'				AND
	[A].[name] = 'LAWYER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------
------------------------------------------------ REGISTER_CASE ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: USER role + CUSTOMER attribute -> REGISTER_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'REGISTER_CASE' AND
	[R].[name] = 'USER'	         AND
	[A].[name] = 'CUSTOMER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-- Grant: USER role + LAWYER attribute -> REGISTER_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name]  = 'REGISTER_CASE' AND
	[R].[name]  = 'USER'		  AND
	[A].[name]  = 'LAWYER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------
------------------------------------------------ REGISTER_USER ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: USER role (any attribute) -> REGISTER_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'REGISTER_USER' AND
	[R].[name] = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ ASSIGN_LAWYER_OWN_CASE ------------------------------------------------
------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role + CUSTOMER attribute -> ASSIGN_LAWYER_OWN_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'ASSIGN_LAWYER_OWN_CASE' AND
	[R].[name] = 'USER'					  AND
	[A].[name] = 'CUSTOMER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-- Grant: USER role + LAWYER attribute -> ASSIGN_LAWYER_OWN_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'ASSIGN_LAWYER_OWN_CASE' AND
	[R].[name] = 'USER'					  AND
	[A].[name] = 'LAWYER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

--------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ ASSIGN_CUSTOMER_OWN_CASE ------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role + CUSTOMER attribute -> ASSIGN_CUSTOMER_OWN_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'ASSIGN_CUSTOMER_OWN_CASE' AND
	[R].[name] = 'USER'						AND
	[A].[name] = 'CUSTOMER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-- Grant: USER role + LAWYER attribute -> ASSIGN_CUSTOMER_OWN_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'ASSIGN_CUSTOMER_OWN_CASE' AND
	[R].[name] = 'USER'						AND
	[A].[name] = 'LAWYER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

----------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ GRANT_PERMISSIONS_OWN_CASE ------------------------------------------------
----------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role + CUSTOMER attribute -> GRANT_PERMISSIONS_OWN_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'GRANT_PERMISSIONS_OWN_CASE' AND
	[R].[name] = 'USER'						  AND
	[A].[name] = 'CUSTOMER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-- Grant: USER role + LAWYER attribute -> GRANT_PERMISSIONS_OWN_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'GRANT_PERMISSIONS_OWN_CASE' AND
	[R].[name] = 'USER'						  AND
	[A].[name] = 'LAWYER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-----------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ REVOKE_PERMISSIONS_OWN_CASE ------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role + CUSTOMER attribute -> REVOKE_PERMISSIONS_OWN_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'REVOKE_PERMISSIONS_OWN_CASE' AND
	[R].[name] = 'USER'						   AND
	[A].[name] = 'CUSTOMER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-- Grant: USER role + LAWYER attribute -> REVOKE_PERMISSIONS_OWN_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], [A].[id]
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE
	[P].[name] = 'REVOKE_PERMISSIONS_OWN_CASE' AND
	[R].[name] = 'USER'						   AND
	[A].[name] = 'LAWYER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-------------------------------------------------------------------------------------------------------
------------------------------------------------ ADMIN ------------------------------------------------
-------------------------------------------------------------------------------------------------------

---------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_ANY_CASE ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> VIEW_ANY_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'VIEW_ANY_CASE' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ ASSIGN_LAWYER_ANY_CASE ------------------------------------------------
------------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> ASSIGN_LAWYER_ANY_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'ASSIGN_LAWYER_ANY_CASE' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

--------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ ASSIGN_CUSTOMER_ANY_CASE ------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> ASSIGN_CUSTOMER_ANY_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'ASSIGN_CUSTOMER_ANY_CASE' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ GRANT_PERMISSIONS_ANY_CASE ------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> GRANT_PERMISSIONS_ANY_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'GRANT_PERMISSIONS_ANY_CASE' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

----------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ REVOKE_PERMISSIONS_ANY_CASE ------------------------------------------------
----------------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> REVOKE_PERMISSIONS_ANY_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'REVOKE_PERMISSIONS_ANY_CASE' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;


---------------------------------------------------------------------------------------------------------------
------------------------------------------------ REGISTER_CASE ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> REGISTER_CASE permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'REGISTER_CASE' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------
------------------------------------------------ REGISTER_USER ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> EDIT_ANY_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'REGISTER_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-----------------------------------------------------------------------------------------------------------------
------------------------------------------------ EDIT_ANY_USER ------------------------------------------------
-----------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> EDIT_ANY_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'EDIT_ANY_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ EDIT_ANY_LAWYER_ACCOUNT_USER ------------------------------------------------
------------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> EDIT_ANY_LAWYER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'EDIT_ANY_LAWYER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

--------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ EDIT_ANY_CUSTOMER_ACCOUNT_USER ------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> EDIT_ANY_CUSTOMER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'EDIT_ANY_CUSTOMER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_ANY_LAWYER_ACCOUNT_USER ------------------------------------------------
------------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> VIEW_ANY_LAWYER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'VIEW_ANY_LAWYER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;


--------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_ANY_CUSTOMER_ACCOUNT_USER ------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------

-- Grant: USER role -> VIEW_ANY_CUSTOMER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R], [attributes] [A]
WHERE [P].[name]  = 'VIEW_ANY_CUSTOMER_ACCOUNT_USER' AND
	  [R].[name]  = 'USER'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

-------------------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER ------------------------------------------------
-------------------------------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'EDIT_ANY_LAWYER_ACCOUNT_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER ------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'EDIT_ANY_CUSTOMER_ACCOUNT_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;


--------------------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER ------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

----------------------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER ------------------------------------------------
----------------------------------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ EDIT_ANY_LAWYER_ACCOUNT_USER ------------------------------------------------
------------------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> EDIT_ANY_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'EDIT_ANY_LAWYER_ACCOUNT_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

--------------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ EDIT_ANY_CUSTOMER_ACCOUNT_USER ------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> EDIT_ANY_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'EDIT_ANY_CUSTOMER_ACCOUNT_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------
------------------------------------------------ VIEW_ANY_USER ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> VIEW_ANY_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'VIEW_ANY_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------
------------------------------------------------ CHAT_ANY_USER ------------------------------------------------
---------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> CHAT_ANY_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'CHAT_ANY_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

---------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ GRANT_PERMISSIONS_ANY_USER ------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> GRANT_PERMISSIONS_ANY_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'GRANT_PERMISSIONS_ANY_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;

----------------------------------------------------------------------------------------------------------------------------
------------------------------------------------ REVOKE_PERMISSIONS_ANY_USER ------------------------------------------------
----------------------------------------------------------------------------------------------------------------------------

-- Grant: ADMIN role (any attribute) -> REVOKE_PERMISSIONS_ANY_USER permission

INSERT INTO [permission_grants] ([permission_id], [role_id], [attribute_id])
SELECT [P].[id], [R].[id], NULL
FROM [permissions] [P], [roles] [R]
WHERE
	[P].[name] = 'REVOKE_PERMISSIONS_ANY_USER' AND
	[R].[name] = 'ADMIN'
ON CONFLICT([permission_id], [role_id], [attribute_id]) DO NOTHING;



/* ========================================================================== */
/*                     END OF PERMISSION ARCHITECTURE                         */
/* ========================================================================== */

COMMIT;");

		return resultContructor.Build();
    }
}
