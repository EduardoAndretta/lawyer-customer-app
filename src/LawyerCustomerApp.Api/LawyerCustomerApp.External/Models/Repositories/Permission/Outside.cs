namespace LawyerCustomerApp.External.Models.Permission;


public static class Permissions
{
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
                                      | REGISTER_CUSTOMER_ACCOUNT_USER						| REGISTER_CUSTOMER_ACCOUNT_USER									    |
                                      | REGISTER_LAWYER_ACCOUNT_USER						| REGISTER_LAWYER_ACCOUNT_USER										    |
                                      | EDIT_OWN_USER										| EDIT_ANY_USER											     			|
                                      | EDIT_OWN_LAWYER_ACCOUNT_USER						| EDIT_ANY_LAWYER_ACCOUNT_USER											|
                                      | EDIT_OWN_CUSTOMER_ACCOUNT_USER						| EDIT_ANY_CUSTOMER_ACCOUNT_USER										|
                                      | VIEW_PUBLIC_USER									| VIEW_ANY_USER															|
                                      | VIEW_PUBLIC_LAWYER_ACCOUNT_USER						| VIEW_ANY_LAWYER_ACCOUNT_USER											|
                                      | VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER					| VIEW_ANY_CUSTOMER_ACCOUNT_USER										|
                                      | VIEW_PERMISSIONS_OWN_USER					        | VIEW_PERMISSIONS_ANY_USER												|
                                      | VIEW_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER			| VIEW_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER								|
                                      | VIEW_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER			| VIEW_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER							|
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
									  | VIEW_PERMISSIONS_USER								| VIEW_PERMISSIONS_ANY_USER												|
									  | VIEW_PERMISSIONS_LAWYER_ACCOUNT_USER				| VIEW_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER								|
									  | VIEW_PERMISSIONS_CUSTOMER_ACCOUNT_USER				| VIEW_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER							|
                                      | EDIT_USER											| EDIT_ANY_USER											     			|
                                      | EDIT_LAWYER_ACCOUNT_USER							| EDIT_ANY_LAWYER_ACCOUNT_USER											|
                                      | EDIT_CUSTOMER_ACCOUNT_USER							| EDIT_ANY_CUSTOMER_ACCOUNT_USER										|
         [CASE (NOT ACL) PERMISSIONS] |-----------------------------------------------------------------------------------------------------------------------------|
                                      | REGISTER_CASE										| REGISTER_CASE															|
                                      | EDIT_OWN_CASE										| EDIT_ANY_CASE															|
                                      | VIEW_OWN_CASE										| VIEW_ANY_CASE															|
                                      | VIEW_PUBLIC_CASE									| VIEW_PUBLIC_CASE														|
                                      | VIEW_PERMISSIONS_OWN_CASE							| VIEW_PERMISSIONS_ANY_CASE												|
                                      | ASSIGN_LAWYER_OWN_CASE								| ASSIGN_LAWYER_ANY_CASE												|
                                      | ASSIGN_CUSTOMER_OWN_CASE							| ASSIGN_CUSTOMER_ANY_CASE												|
                                      | GRANT_PERMISSIONS_OWN_CASE							| GRANT_PERMISSIONS_ANY_CASE											|
                                      | REVOKE_PERMISSIONS_OWN_CASE							| REVOKE_PERMISSIONS_ANY_CASE											|
             [CASE (ACL) PERMISSIONS] |-----------------------------------------------------------------------------------------------------------------------------|	
                                      | EDIT_CASE											| EDIT_ANY_CASE															|
                                      | VIEW_CASE											| VIEW_ANY_CASE															|
                                      | VIEW_PERMISSIONS_CASE							    | VIEW_PERMISSIONS_ANY_CASE												|
									  | ASSIGN_LAWYER_CASE									| ASSIGN_LAWYER_ANY_CASE												|
                                      | ASSIGN_CUSTOMER_CASE								| ASSIGN_CUSTOMER_ANY_CASE												|
                                      | GRANT_PERMISSIONS_CASE								| GRANT_PERMISSIONS_ANY_CASE											|
                                      | REVOKE_PERMISSIONS_CASE								| REVOKE_PERMISSIONS_ANY_CASE											|
    */


    /// <summary>
    /// CASE
    /// </summary>

    // =================== [   ACL   ] =================== //

    public const string EDIT_CASE = "EDIT_CASE";

    public const string VIEW_CASE = "VIEW_CASE";

    public const string VIEW_PERMISSIONS_CASE = "VIEW_PERMISSIONS_CASE";

    public const string ASSIGN_LAWYER_CASE = "ASSIGN_LAWYER_CASE";

    public const string ASSIGN_CUSTOMER_CASE = "ASSIGN_CUSTOMER_CASE";

    public const string GRANT_PERMISSIONS_CASE = "GRANT_PERMISSIONS_CASE";

    public const string REVOKE_PERMISSIONS_CASE = "REVOKE_PERMISSIONS_CASE";

    // =================== [ NOT ACL ] =================== //

    public const string REGISTER_CASE = "REGISTER_CASE";

    public const string EDIT_OWN_CASE = "EDIT_OWN_CASE";

    public const string VIEW_OWN_CASE = "VIEW_OWN_CASE";

    public const string VIEW_PUBLIC_CASE = "VIEW_PUBLIC_CASE";

    public const string VIEW_PERMISSIONS_OWN_CASE = "VIEW_PERMISSIONS_OWN_CASE";

    public const string ASSIGN_LAWYER_OWN_CASE = "ASSIGN_LAWYER_OWN_CASE";

    public const string ASSIGN_CUSTOMER_OWN_CASE = "ASSIGN_CUSTOMER_OWN_CASE";


    public const string GRANT_PERMISSIONS_OWN_CASE = "GRANT_PERMISSIONS_OWN_CASE";

    public const string REVOKE_PERMISSIONS_OWN_CASE = "REVOKE_PERMISSIONS_OWN_CASE";

    /// <summary>
    /// RELATIONSHIP
    /// </summary>

    // =================== [   ACL   ] =================== //

    public const string VIEW_USER = "VIEW_USER";

    public const string VIEW_LAWYER_ACCOUNT_USER = "VIEW_LAWYER_ACCOUNT_USER";

    public const string VIEW_CUSTOMER_ACCOUNT_USER = "VIEW_CUSTOMER_ACCOUNT_USER";


    public const string VIEW_PERMISSIONS_USER = "VIEW_PERMISSIONS_USER";

    public const string VIEW_PERMISSIONS_LAWYER_ACCOUNT_USER = "VIEW_PERMISSIONS_LAWYER_ACCOUNT_USER";

    public const string VIEW_PERMISSIONS_CUSTOMER_ACCOUNT_USER = "VIEW_PERMISSIONS_CUSTOMER_ACCOUNT_USER";


    public const string MANAGE_USER = "MANAGE_USER";

    public const string CHAT_USER = "CHAT_USER";


    public const string EDIT_USER = "EDIT_USER";

    public const string EDIT_LAWYER_ACCOUNT_USER = "EDIT_LAWYER_ACCOUNT_USER";

    public const string EDIT_CUSTOMER_ACCOUNT_USER = "EDIT_CUSTOMER_ACCOUNT_USER";


    public const string GRANT_PERMISSIONS_USER = "GRANT_PERMISSIONS_USER";

    public const string REVOKE_PERMISSIONS_USER = "REVOKE_PERMISSIONS_USER";


    public const string GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER = "GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER";

    public const string REVOKE_PERMISSIONS_LAWYER_ACCOUNT_USER = "REVOKE_PERMISSIONS_LAWYER_ACCOUNT_USER";


    public const string GRANT_PERMISSIONS_CUSTOMER_ACCOUNT_USER = "GRANT_PERMISSIONS_CUSTOMER_ACCOUNT_USER";

    public const string REVOKE_PERMISSIONS_CUSTOMER_ACCOUNT_USER = "REVOKE_PERMISSIONS_CUSTOMER_ACCOUNT_USER";

    // =================== [ NOT ACL ] =================== //

    public const string VIEW_OWN_USER = "VIEW_OWN_USER";

    public const string VIEW_OWN_LAWYER_ACCOUNT_USER = "VIEW_OWN_LAWYER_ACCOUNT_USER";

    public const string VIEW_OWN_CUSTOMER_ACCOUNT_USER = "VIEW_OWN_CUSTOMER_ACCOUNT_USER";


    public const string VIEW_PUBLIC_USER = "VIEW_PUBLIC_USER";

    public const string VIEW_PUBLIC_LAWYER_ACCOUNT_USER = "VIEW_PUBLIC_LAWYER_ACCOUNT_USER";

    public const string VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER = "VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER";


    public const string VIEW_PERMISSIONS_OWN_USER = "VIEW_PERMISSIONS_OWN_USER";

    public const string VIEW_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER = "VIEW_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER";

    public const string VIEW_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER = "VIEW_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER";


    public const string REGISTER_USER = "REGISTER_USER";

    public const string REGISTER_CUSTOMER_ACCOUNT_USER = "REGISTER_CUSTOMER_ACCOUNT_USER";

    public const string REGISTER_LAWYER_ACCOUNT_USER = "REGISTER_LAWYER_ACCOUNT_USER";


    public const string EDIT_OWN_USER = "EDIT_OWN_USER";

    public const string EDIT_OWN_LAWYER_ACCOUNT_USER = "EDIT_OWN_LAWYER_ACCOUNT_USER";

    public const string EDIT_OWN_CUSTOMER_ACCOUNT_USER = "EDIT_OWN_CUSTOMER_ACCOUNT_USER";


    public const string GRANT_PERMISSIONS_OWN_USER = "GRANT_PERMISSIONS_OWN_USER";

    public const string GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER = "GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER";

    public const string GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER = "GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER";


    public const string REVOKE_PERMISSIONS_OWN_USER = "REVOKE_PERMISSIONS_OWN_USER";

    public const string REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER = "REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER";

    public const string REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER = "REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER";

    // [ADMINISTRATOR PERMISSIONS]

    /// <summary>
    /// CASE
    /// </summary>

    // =================== [   ACL   ] =================== //

    public const string EDIT_ANY_CASE = "EDIT_ANY_CASE";

    public const string VIEW_ANY_CASE = "VIEW_ANY_CASE";

    public const string VIEW_PERMISSIONS_ANY_CASE = "VIEW_PERMISSIONS_ANY_CASE";

    public const string ASSIGN_LAWYER_ANY_CASE = "ASSIGN_LAWYER_ANY_CASE";

    public const string ASSIGN_CUSTOMER_ANY_CASE = "ASSIGN_CUSTOMER_ANY_CASE";

    public const string GRANT_PERMISSIONS_ANY_CASE = "GRANT_PERMISSIONS_ANY_CASE";

    public const string REVOKE_PERMISSIONS_ANY_CASE = "REVOKE_PERMISSIONS_ANY_CASE";

    /// <summary>
    /// RELATIONSHIP
    /// </summary>

    // =================== [   ACL   ] =================== //

    public const string VIEW_ANY_USER = "VIEW_ANY_USER";

    public const string VIEW_ANY_LAWYER_ACCOUNT_USER = "VIEW_ANY_LAWYER_ACCOUNT_USER";

    public const string VIEW_ANY_CUSTOMER_ACCOUNT_USER = "VIEW_ANY_CUSTOMER_ACCOUNT_USER";


    public const string VIEW_PERMISSIONS_ANY_USER = "VIEW_PERMISSIONS_ANY_USER";

    public const string VIEW_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER = "VIEW_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER";

    public const string VIEW_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER = "VIEW_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER";


    public const string EDIT_ANY_USER = "EDIT_ANY_USER";

    public const string EDIT_ANY_LAWYER_ACCOUNT_USER = "EDIT_ANY_LAWYER_ACCOUNT_USER";

    public const string EDIT_ANY_CUSTOMER_ACCOUNT_USER = "EDIT_ANY_CUSTOMER_ACCOUNT_USER";


    public const string CHAT_ANY_USER = "CHAT_ANY_USER";


    public const string GRANT_PERMISSIONS_ANY_USER = "GRANT_PERMISSIONS_ANY_USER";

    public const string REVOKE_PERMISSIONS_ANY_USER = "REVOKE_PERMISSIONS_ANY_USER";


    public const string GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER = "GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER";

    public const string REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER = "REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER";


    public const string GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER = "GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER";

    public const string REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER = "REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER";
}