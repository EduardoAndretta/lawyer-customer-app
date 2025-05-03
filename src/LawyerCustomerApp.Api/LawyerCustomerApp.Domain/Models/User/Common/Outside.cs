using LawyerCustomerApp.External.Extensions;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Dynamic;
using System.Numerics;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.Json.Nodes;
using static LawyerCustomerApp.Domain.User.Common.Models.EditParameters;

namespace LawyerCustomerApp.Domain.User.Common.Models;

public class SearchParametersDto
{
    public int? UserId { get; init; }
    public int? AttributeId { get; init; }
    public int? RoleId { get; init; }

    public string? Query { get; init; }

    public PaginationProperties? Pagination { get; init; }

    public SearchParameters ToOrdinary()
    {
        return new SearchParameters
        {
            Query = this.Query ?? string.Empty,

            UserId      = this.UserId      ?? 0,
            AttributeId = this.AttributeId ?? 0,
            RoleId      = this.RoleId      ?? 0,

            Pagination = new() 
            {
                Begin = this.Pagination?.Begin ?? 0,
                End   = this.Pagination?.End   ?? 0
            },
        };
    }

    public class PaginationProperties
    {
        public int? Begin { get; init; }
        public int? End { get; init; }
    }
}

public class SearchParameters
{
    public required int UserId { get; init; }
    public required int AttributeId { get; init; }
    public required int RoleId { get; init; }

    public required string Query { get; init; }

    public required PaginationProperties Pagination { get; init; }

    public class PaginationProperties
    {
        public required int Begin { get; init; }
        public required int End { get; init; }
    }
}

public class RegisterParametersDto
{
    public int? RoleId { get; init; }

    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? Name { get; init; }

    public RegisterParameters ToOrdinary()
    {
        return new RegisterParameters
        {
            RoleId = this.RoleId ?? 0,

            Email    = this.Email    ?? string.Empty,
            Password = this.Password ?? string.Empty,
            Name     = this.Name     ?? string.Empty
        };
    }
}

public class RegisterParameters
{
    public required int RoleId { get; init; }

    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string Name { get; init; }
}

public class EditParametersDto
{
    public int? RelatedUserId { get; init; }

    public int? UserId { get; init; }
    public int? RoleId { get; init; }

    public JsonNode? Values { get; init; }


    public EditParameters ToOrdinary()
    {
        static PatchField<string?> TryParseToString(JsonNode? node)
        {
            if (node == null)
                return new PatchField<string?>();

            var valueKind = node.GetValueKind();

            if (valueKind != JsonValueKind.String)
                return new PatchField<string?>();

            var value = node.GetValue<object>();

            return new PatchField<string?>()
            {
                Received = true,
                Value    = node.GetValue<string?>()
            };
        }

        static PatchField<bool?> TryParseToBoolean(JsonNode? node)
        {
            if (node == null)
                return new PatchField<bool?>();

            var valueKind = node.GetValueKind();

            if (valueKind != JsonValueKind.True && valueKind != JsonValueKind.False)
                return new PatchField<bool?>();

            return new PatchField<bool?>()
            {
                Received = true,
                Value    = node.GetValue<bool?>()
            };
        }

        if (this.Values == null)
        {
            return new EditParameters
            {
                RelatedUserId = this.RelatedUserId ?? 0,

                UserId = this.UserId ?? 0,
                RoleId = this.RoleId ?? 0
            };
        }

        return new EditParameters
        {
            RelatedUserId = this.RelatedUserId ?? 0,

            UserId = this.UserId ?? 0,
            RoleId = this.RoleId ?? 0,

            Private = TryParseToBoolean(this.Values["private"]),

            Address = new()
            {
                ZipCode     = TryParseToString(this.Values["address"]?["zipCode"]),
                HouseNumber = TryParseToString(this.Values["address"]?["houseNumber"]),
                Complement  = TryParseToString(this.Values["address"]?["complement"]),
                District    = TryParseToString(this.Values["address"]?["district"]),
                City        = TryParseToString(this.Values["address"]?["city"]),
                State       = TryParseToString(this.Values["address"]?["state"]),
                Country     = TryParseToString(this.Values["address"]?["country"]),
            },

            Document = new()
            {
                Type               = TryParseToString(this.Values["document"]?["type"]),
                IdentifierDocument = TryParseToString(this.Values["document"]?["identifierDocument"])
            },

            Accounts = new()
            {
                Lawyer = new()
                {
                    Private = TryParseToBoolean(this.Values["attributes"]?["lawyer"]?["private"]),

                    Phone = TryParseToString(this.Values["attributes"]?["lawyer"]?["phone"]),

                    Address = new()
                    {
                        ZipCode     = TryParseToString(this.Values["attributes"]?["lawyer"]?["address"]?["zipCode"]),
                        HouseNumber = TryParseToString(this.Values["attributes"]?["lawyer"]?["address"]?["houseNumber"]),
                        Complement  = TryParseToString(this.Values["attributes"]?["lawyer"]?["address"]?["complement"]),
                        District    = TryParseToString(this.Values["attributes"]?["lawyer"]?["address"]?["district"]),
                        City        = TryParseToString(this.Values["attributes"]?["lawyer"]?["address"]?["city"]),
                        State       = TryParseToString(this.Values["attributes"]?["lawyer"]?["address"]?["state"]),
                        Country     = TryParseToString(this.Values["attributes"]?["lawyer"]?["address"]?["country"])
                    },

                    Document = new()
                    {
                        Type               = TryParseToString(this.Values["attributes"]?["lawyer"]?["document"]?["type"]),
                        IdentifierDocument = TryParseToString(this.Values["attributes"]?["lawyer"]?["document"]?["identifierDocument"])
                    }
                },

                Customer = new()
                {
                    Private = TryParseToBoolean(this.Values["attributes"]?["customer"]?["private"]),

                    Phone = TryParseToString(this.Values["attributes"]?["customer"]?["phone"]),

                    Address = new()
                    {
                        ZipCode     = TryParseToString(this.Values["attributes"]?["customer"]?["address"]?["zipCode"]),
                        HouseNumber = TryParseToString(this.Values["attributes"]?["customer"]?["address"]?["houseNumber"]),
                        Complement  = TryParseToString(this.Values["attributes"]?["customer"]?["address"]?["complement"]),
                        District    = TryParseToString(this.Values["attributes"]?["customer"]?["address"]?["district"]),
                        City        = TryParseToString(this.Values["attributes"]?["customer"]?["address"]?["city"]),
                        State       = TryParseToString(this.Values["attributes"]?["customer"]?["address"]?["state"]),
                        Country     = TryParseToString(this.Values["attributes"]?["customer"]?["address"]?["country"])
                    },

                    Document = new()
                    {
                        Type               = TryParseToString(this.Values["attributes"]?["customer"]?["document"]?["type"]),
                        IdentifierDocument = TryParseToString(this.Values["attributes"]?["customer"]?["document"]?["identifierDocument"])
                    }
                }
            }
        };
    }
}

public record EditParameters
{
    public class PatchField<T>
    {
        public bool Received { get; init; } = false;
        public T? Value { get; init; }
    }

    public bool HasChanges
    {
        get
        {
            return Private.Received;
        }
    }

    public required int RelatedUserId { get; init; }

    public required int UserId { get; init; }
    public required int RoleId { get; init; }

    public PatchField<bool?> Private { get; init; } = new();

    public AddressProperties Address { get; init; } = new();
    public DocumentProperties Document { get; init; } = new();

    public AttributeInformationsProperties Accounts { get; init; } = new();

    public class DocumentProperties
    {
        public bool HasChanges
        {
            get
            {
                return Type.Received || IdentifierDocument.Received;
            }
        }

        public PatchField<string?> Type { get; init; } = new();
        public PatchField<string?> IdentifierDocument { get; init; } = new();
    }

    public class AddressProperties
    {
        public bool HasChanges 
        { 
            get 
            {
                return ZipCode.Received || HouseNumber.Received || Complement.Received || District.Received || City.Received || State.Received || Country.Received;
            } 
        }

        public PatchField<string?> ZipCode { get; init; } = new();
        public PatchField<string?> HouseNumber { get; init; } = new();
        public PatchField<string?> Complement { get; init; } = new();
        public PatchField<string?> District { get; init; } = new();
        public PatchField<string?> City { get; init; } = new();
        public PatchField<string?> State { get; init; } = new();
        public PatchField<string?> Country { get; init; } = new();
    }

    public class AttributeInformationsProperties
    {
        public LawyerInformationsProperties Lawyer { get; init; } = new();
        public CustomerInformationsProperties Customer { get; init; } = new();

        public class LawyerInformationsProperties
        {
            public bool HasChanges
            {
                get
                {
                    return Phone.Received || Private.Received;
                }
            }

            public PatchField<string?> Phone { get; init; } = new();
            public PatchField<bool?> Private { get; init; } = new();

            public AddressProperties Address { get; init; } = new();
            public DocumentProperties Document { get; init; } = new();
        }

        public class CustomerInformationsProperties
        {
            public bool HasChanges
            {
                get
                {
                    return Phone.Received || Private.Received;
                }
            }

            public PatchField<string?> Phone { get; init; } = new();
            public PatchField<bool?> Private { get; init; } = new();

            public AddressProperties Address { get; init; } = new();
            public DocumentProperties Document { get; init; } = new();
        }
    }
}

public class GrantPermissionsParametersDto
{
    public int? RelatedUserId { get; init; }
    public int? UserId { get; init; }
    public int? AttributeId { get; init; }
    public int? RoleId { get; init; }

    public IEnumerable<PermissionProperties?>? Permissions { get; init; }

    public class PermissionProperties
    {
        public int? AttributeId { get; init; }
        public int? PermissionId { get; init; }
        public int? UserId { get; init; }
        public int? RoleId { get; init; }
    }

    public GrantPermissionsParameters ToOrdinary()
    {
        return new GrantPermissionsParameters
        {
            RelatedUserId = this.RelatedUserId ?? 0,
            UserId        = this.UserId        ?? 0,
            AttributeId   = this.AttributeId   ?? 0,
            RoleId        = this.RoleId        ?? 0,

            Permissions = this.Permissions?.Select(item =>
                new GrantPermissionsParameters.PermissionProperties
                {
                    AttributeId  = item?.AttributeId  ?? 0,
                    PermissionId = item?.PermissionId ?? 0,
                    UserId       = item?.UserId       ?? 0,
                    RoleId       = item?.RoleId       ?? 0,
                }) 
            ?? new Collection<GrantPermissionsParameters.PermissionProperties>()
        };
    }
}

public class GrantPermissionsParameters
{
    public required int RelatedUserId { get; init; }
    public required int UserId { get; init; }
    public required int AttributeId { get; init; }
    public required int RoleId { get; init; }

    public required IEnumerable<PermissionProperties> Permissions { get; init; }

    public class PermissionProperties
    {
        public readonly Guid Id = Guid.NewGuid();

        public required int AttributeId { get; init; }
        public required int PermissionId { get; init; }
        public required int UserId { get; init; }
        public required int RoleId { get; init; }
    }
}

public class RevokePermissionsParametersDto
{
    public int? RelatedUserId { get; init; }
    public int? UserId { get; init; }
    public int? AttributeId { get; init; }
    public int? RoleId { get; init; }

    public IEnumerable<PermissionProperties?>? Permissions { get; init; }

    public class PermissionProperties
    {
        public int? AttributeId { get; init; }
        public int? PermissionId { get; init; }
        public int? UserId { get; init; }
        public int? RoleId { get; init; }
    }

    public RevokePermissionsParameters ToOrdinary()
    {
        return new RevokePermissionsParameters
        {
            RelatedUserId = this.RelatedUserId ?? 0,
            UserId        = this.UserId        ?? 0,
            AttributeId   = this.AttributeId   ?? 0,
            RoleId        = this.RoleId        ?? 0,

            Permissions = this.Permissions?.Select(item =>
                new RevokePermissionsParameters.PermissionProperties
                {
                    AttributeId  = item?.AttributeId  ?? 0,
                    PermissionId = item?.PermissionId ?? 0,
                    UserId       = item?.UserId       ?? 0,
                    RoleId       = item?.RoleId       ?? 0,
                }) 
            ?? new Collection<RevokePermissionsParameters.PermissionProperties>()
        };
    }
}

public class RevokePermissionsParameters
{
    public required int RelatedUserId { get; init; }
    public required int UserId { get; init; }
    public required int AttributeId { get; init; }
    public required int RoleId { get; init; }

    public required IEnumerable<PermissionProperties> Permissions { get; init; }

    public class PermissionProperties
    {
        public readonly Guid Id = Guid.NewGuid();

        public required int AttributeId { get; init; }
        public required int PermissionId { get; init; }
        public required int UserId { get; init; }
        public required int RoleId { get; init; }
    }
}



public record SearchInformationDto
{
    public IEnumerable<ItemProperties>? Items { get; init; }

    public class ItemProperties
    {
        public string? Name { get; init; }

        public int? Id { get; init; }

        public int? CustomerId { get; init; }
        public int? LawyerId { get; init; }

        public bool? HasCustomerAccount { get; init; }
        public bool? HasLawyerAccount { get; init; }
    }
}

public record SearchInformation
{
    public required IEnumerable<ItemProperties> Items { get; init; }

    public class ItemProperties
    {
        public required string Name { get; init; }

        public required int Id { get; init; }

        public int? CustomerId { get; init; }
        public int? LawyerId { get; init; }

        public required bool HasCustomerAccount { get; init; }
        public required bool HasLawyerAccount { get; init; }
    }

    public SearchInformationDto ToOrdinary()
    {
        return new SearchInformationDto
        {
            Items = this.Items.Select(x =>
                new SearchInformationDto.ItemProperties
                {
                    Name = x.Name,

                    Id = x.Id,

                    CustomerId = x.CustomerId,
                    LawyerId   = x.LawyerId,

                    HasLawyerAccount   = x.HasLawyerAccount,
                    HasCustomerAccount = x.HasCustomerAccount
                })
        };
    }
}