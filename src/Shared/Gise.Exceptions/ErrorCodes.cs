namespace Gise.Exceptions;

public static class ErrorCodes
{
    public static class General
    {
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string InternalError = "INTERNAL_SERVER_ERROR";
    }

    public static class Auth
    {
        public const string MissingClaim = "AUTH_MISSING_CLAIM";
    }

    public static class Venue
    {
        public const string NotFound = "VENUE_NOT_FOUND";
    }

    public static class Event
    {
        public const string NotFound = "EVENT_NOT_FOUND";
        public const string NotOwner = "EVENT_NOT_OWNER";
        public const string Cancelled = "EVENT_CANCELLED";
        public const string AlreadyPublished = "EVENT_ALREADY_PUBLISHED";
        public const string NotPublished = "EVENT_NOT_PUBLISHED";
        public const string AlreadyStarted = "EVENT_ALREADY_STARTED";
    }

    public static class Price
    {
        public const string SectionNotInVenue = "PRICE_SECTION_NOT_IN_VENUE";
        public const string MissingSections = "PRICE_MISSING_SECTIONS";
    }
}
