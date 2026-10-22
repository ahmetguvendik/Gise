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
        public const string HasEvents = "VENUE_HAS_EVENTS";
    }

    public static class Event
    {
        public const string NotFound = "EVENT_NOT_FOUND";
        public const string NotOwner = "EVENT_NOT_OWNER";
        public const string Cancelled = "EVENT_CANCELLED";
        public const string AlreadyPublished = "EVENT_ALREADY_PUBLISHED";
        public const string NotPublished = "EVENT_NOT_PUBLISHED";
        public const string AlreadyStarted = "EVENT_ALREADY_STARTED";
        public const string NotDraft = "EVENT_NOT_DRAFT";
        public const string VenueLocked = "EVENT_VENUE_LOCKED";
        public const string InReview = "EVENT_IN_REVIEW";
        public const string NotInReview = "EVENT_NOT_IN_REVIEW";
        public const string AdminOnly = "EVENT_ADMIN_ONLY";
    }

    public static class Media
    {
        public const string TypeNotAllowed = "MEDIA_TYPE_NOT_ALLOWED";
        public const string TooLarge = "MEDIA_TOO_LARGE";
        public const string InvalidKey = "MEDIA_INVALID_KEY";
        public const string NotFound = "MEDIA_NOT_FOUND";
        public const string CorruptContent = "MEDIA_CORRUPT_CONTENT";
    }

    public static class Stock
    {
        public const string NotFound = "STOCK_NOT_FOUND";
    }

    public static class Price
    {
        public const string SectionNotInVenue = "PRICE_SECTION_NOT_IN_VENUE";
        public const string MissingSections = "PRICE_MISSING_SECTIONS";
    }
}
