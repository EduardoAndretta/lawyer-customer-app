namespace LawyerCustomerApp.External.Models.Permission;

public static class Permissions
{

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

    // [ORDINARY PERMISSIONS]

    public const string CHAT_USER       = "CHAT_USER";
    public const string MANAGE_OWN_USER = "MANAGE_OWN_USER";

    // [CASE (SPECIFIC) PERMISSIONS]

    public const string VIEW_OWN_CASES        = "VIEW_OWN_CASES";
    public const string VIEW_ANY_PUBLIC_CASE  = "VIEW_ANY_PUBLIC_CASE";
    public const string REGISTER_CASE         = "REGISTER_CASE";
    public const string ASSIGN_LAWYER_CASE    = "ASSIGN_LAWYER_CASE";
    public const string ASSIGN_CUSTOMER_CASE  = "ASSIGN_CUSTOMER_CASE";

    // [ADMINISTRATOR PERMISSIONS]

    public const string CHAT_ANY_USER   = "CHAT_ANY_USER";
    public const string MANAGE_ANY_USER = "MANAGE_ANY_USER";

    // [ADMINISTRATOR PERMISSIONS] [CASE (SPECIFIC) PERMISSIONS]

    public const string VIEW_ANY_CASE     = "VIEW_ANY_CASE";
    public const string REGISTER_ANY_CASE = "REGISTER_ANY_CASE";
    public const string EDIT_ANY_CASE     = "EDIT_ANY_CASE";
}