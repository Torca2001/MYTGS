using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firefly
{
    class FullTask
    {
        public DescriptionDetails descriptionDetails;
        public bool hideFromRecipients;
        public string responseReleaseMode;
        public string pseudoFromGuid;
        public string pseudoToGuid;
        public string title;
        public DateTime setDate;
        public DateTime dueDate;
        public TaskSetter setter;
        public bool archived;
        public bool draft;
        public bool hiddenFromParentPortal;
        public bool hideAddresses;
        public bool markbookHidden;
        public bool markbookHighlight;
        public string markbookDisplaymode;
        public int assessmentType;
        public int rubricId;
        public int assessmentDetailsId;
        public bool fileSubmissionRequired;
        public string taskType;
        public int pageId;
        public float totalMarkOutOf;
        public string descriptionPageUrl;
        public TaskSetter[] coowners;
        public FileAttachments[] fileAttachments;
        public PageAttachments[] pageAttachments;
        public Address[] addressees;
        public RecipientResponse[] recipientsResponses;
        public RecipientResponse[] allRecipientsResponses;
        public RecipientResponse[] recipientStatuses;
        public bool deleted;
        public bool ownershipRevoked;
        public bool setInTheFuture;
        public int id;
    }

    struct DescriptionDetails
    {
        public int descriptionPageId;
        public string htmlContent;
        public bool containsQuestions;
        public bool isSimpleDescription;
    }

    struct TaskSetter
    {
        public string SortKey;
        public string guid;
        public string name;
        public bool deleted;
    }

    struct FileAttachments
    {
        public int resourceId;
        public string fileName;
        public string fileType;
        public string etag;
        public DateTime dateCreated;
    }

    struct PageAttachments
    {
        public int pageId;
        public string titleLong;
        public string titleShort;
    }

    struct Address
    {
        public bool isGroup;
        public Principal principal;
    }

    struct Principal
    {
        public string sortKey;
        public string guid;
        public string name;
        public bool deleted;
    }
    
    struct RecipientResponse
    {
        public Principal principal;
        public Response[] responses;
    }

    struct Response
    {
        public string latestRead;
        public string authorName;
        public int versionId;
        public bool released;
        public DateTime releasedTimestamp;
        public bool edited;
        public string authorGuid;
        public string eventType;
        public DateTime sentTimestamp;
        public DateTime createdTimestamp;
        public bool deleted;
        public string eventGuid;
        public AssessmentDetails taskAssessmentDetails;

    }

    struct AssessmentDetails
    {
        public float assessmentMarkMax;
        public int assessmentDetailsId;
        public int assessmentType;
    }
}
