using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Firefly
{
    //There is so much inefficiency in these structures, any improvements to the handling of the json responses would be greatly appreciated
    //Task objects
    struct FullTask
    {
        public DescriptionDetails descriptionDetails;
        public bool hideFromRecipients;
        public string responseReleaseMode;
        public string pseudoFromGuid;
        public string pseudoToGuid;
        public string title;
        public DateTime setDate;
        public DateTime dueDate;
        public Principal setter;
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
        public float mark;
        public string descriptionPageUrl;
        public Principal[] coowners;
        public FileAttachments[] fileAttachments;
        public PageAttachments[] pageAttachments;
        public Address[] addressees;
        public RecipientResponse[] recipientsResponses;
        public RecipientResponse[] allRecipientsResponses;
        public RecipientResponse[] recipientStatuses;
        public bool deleted;
        public bool ownershipRevoked;
        public bool setInTheFuture;
        [JsonProperty(Required = Required.Always)]
        public int id;
    }

    struct DescriptionDetails
    {
        public int descriptionPageId;
        public string htmlContent;
        public bool containsQuestions;
        public bool isSimpleDescription;
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
        public bool latestRead;
        public string authorName;
        public float mark;
        public bool isMarkAutomated;
        public string message;
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
        public int assessmentType; //Sometimes a negative int???
    }

    struct Attendee
    {
        public Principal principal;
        public string role;
    }

    struct AttendeePrincipal
    {
        public string guid;
        public string name;
        public string sort_key;
        public Group group;
    }

    //Event objects
    struct Group
    {
        public string guid;
        public string name;
        public string sort_key;
        public Color personal_colour;
    }

    struct Data
    {
        public EventHolder data;
    }

    struct EventHolder
    {
        public FFEvent[] events;
    }

    struct FFEvent
    {
        public string guid;
        public string description;
        public DateTime start;
        public DateTime end;
        public string location;
        public string subject;
        public Attendee[] attendees;
    }

    //Response object

    struct TmpResp
    {
        public Responses responses; //why.....
    }

    struct Responses
    {
        public ResponseEvent[] responses;
        public Dictionary<string, Principal> users;
        //Don't need the included users for this its redundant
    }

    class ResponseEvent
    {
        public Recipient recipient;   
        public int latestVersionId;
        public RespEvent[] events;
        //Useful in putting the events back into their original fulltask objects
        //Why couldn't they just keep the same object structure? I have no clue
        public Response[] ToTaskResponses(Dictionary<string, Principal> U)
        {
            Response[] tmp = new Response[events.Length];
            for (int i = 0; i < events.Length; i++)
            {
                RespEvent item = events[i];
                Response resp = new Response()
                {
                    authorGuid = item.description.author,
                    eventType = item.description.type,
                    sentTimestamp = item.description.sent,
                    eventGuid = item.description.eventGuid,
                    versionId = item.description.eventVersionId,
                    released = item.state.released,
                    releasedTimestamp = item.state.releasedAt,
                    edited = item.state.edited,
                    deleted = item.state.deleted,
                    latestRead = item.state.read,
                    message = string.IsNullOrEmpty(item.description.feedback)?item.description.message:item.description.feedback,
                    mark = item.description.mark,
                    isMarkAutomated = item.description.isMarkAutomated,
                    createdTimestamp = item.description.sent,
                    authorName = U[item.description.author].name,
                };
                //Checks for taskAssessmentDetails
                if (item.description.taskAssessmentDetails != null)
                {
                    resp.taskAssessmentDetails = item.description.taskAssessmentDetails.ToFull();
                }
                tmp[i] = resp;
            }
            return tmp;
        }
    }

    struct Recipient
    {
        public string type;
        public string guid;
    }

    struct RespEvent
    {
        public Description description;
        public RespState state;
    }

    struct Description
    {
        public string type;
        public RespFile[] files;
        public string feedback; //Mark and grade message comes as feedback
        public string message; //Comments come as messages
        public float mark;
        public bool isWorksheet;
        public bool isMarkAutomated;
        public RespAssessmentDetails taskAssessmentDetails;
        public string taskTitle;
        public int eventVersionId;
        public string author;
        public string eventGuid;
        public DateTime sent;
    }

    class RespAssessmentDetails
    {
        public int id;
        public RespAssessmentType assessmentType;
        public int markMax;
        public int rubricId;

        public AssessmentDetails ToFull()
        {
            AssessmentDetails tmp = new AssessmentDetails()
            {
                assessmentDetailsId = id,
                assessmentMarkMax = markMax
            };
            return tmp;
        }
    }

    struct RespAssessmentType
    {
        //ID which I don't know what to do with
        public int id;
        public string name;
        //int[] assessmentActions //This just contains a bunch of numbers which I don't know cause what actions
        public bool isUnspecified;
        public bool isRubric;
        public bool isMark;
        public bool isGrade;
        public bool isFeedback;
        public bool isMarkAndGrade; //Isn't this redudant as checking both isMark and isGrade?
    }

    struct RespFile
    {
        public RespId id;
        public string title;
        public string type;
    }

    struct RespId
    {
        public int value;
    }

    struct RespState
    {
        public bool released;
        public DateTime releasedAt;
        public bool canDelete;
        public bool canEdit;
        public bool edited;
        public bool deleted;
        public bool read;
    }

    //Event objects
    public class ReadArgs : EventArgs
    {
        public string EventId { get; set; }
        public int TaskId { get; set; }
        public string ResponseGuid { get; set; }
    }
}
