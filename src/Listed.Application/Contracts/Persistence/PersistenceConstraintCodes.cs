namespace Listed.Application.Contracts.Persistence;

public static class PersistenceConstraintCodes
{
    public static class EventParticipant
    {
        public const string EventUserUnique = "event_participants.event_user.unique";
    }

    public static class Organisation
    {
        public const string CountryCinUnique = "organisations.country_cin.unique";
    }

    public static class OrganisationMember
    {
        public const string OrganisationUserUnique = "organisation_members.organisation_user.unique";
    }

    public static class User
    {
        public const string EmailUnique = "users.email.unique";
    }

    public static class RefreshToken
    {
        public const string TokenHashUnique = "refresh_tokens.token_hash.unique";
        public const string UserDeviceActiveUnique = "refresh_tokens.user_device_active.unique";
    }

    public static class UserInfo
    {
        public const string NinUnique = "users.nin.unique";
        public const string PhoneNumberUnique = "users.phone_number.unique";
    }

    public static class Common
    {
        public const string UnknownUnique = "persistence.unique.unknown";
    }
}
