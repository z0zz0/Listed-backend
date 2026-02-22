namespace Listed.Infrastructure.Persistence;

public static class PersistenceConstraintNames
{
    public static class Event
    {
        public const string OrganisationIdIndex = "index_events_organisation_id";
    }

    public static class EventParticipant
    {
        public const string EventIdIndex = "index_event_participants_event_id";
        public const string EventUserUnique = "unique_index_event_participants_event_id_user_id";
    }

    public static class EventPhoto
    {
        public const string EventIdIndex = "index_event_photos_event_id";
    }

    public static class Organisation
    {
        public const string CountryCinUnique = "unique_index_organisations_country_cin";
    }

    public static class OrganisationMember
    {
        public const string OrganisationUserUnique = "unique_index_organisation_members_organisation_id_user_id";
    }

    public static class OrganisationPhoto
    {
        public const string OrganisationIdIndex = "index_organisation_photos_organisation_id";
    }

    public static class User
    {
        public const string EmailUnique = "unique_index_users_email";
    }

    public static class RefreshToken
    {
        public const string TokenHashUnique = "unique_index_refresh_tokens_token_hash";
        public const string UserActiveLookup = "index_refresh_tokens_user_id_revoked_at";
        public const string UserDeviceActiveUnique = "unique_index_refresh_tokens_user_id_device_id_active";
    }

    public static class UserInfo
    {
        public const string NinUnique = "unique_index_users_nin";
        public const string PhoneNumberUnique = "unique_index_users_phone_number";
    }

    public static class UserPhoto
    {
        public const string UserIdIndex = "index_user_photos_user_id";
    }
}
