using System;
using System.Runtime.Serialization;
using FhiModel.Common;

namespace FhiModel
{
    [DataContract(Namespace = "", IsReference = true)]
    public class Attributes : ModelBase
    {
        private String _author;
        private String _title;
        private String _notes;
        private DateTime _created;
        private Int32 _wkid;
        private int _assessmentYear;
        private Guid _userId;

        public Attributes()
        {
            Id = Guid.NewGuid();
            OnDeserialized();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            if (AssessmentYear == 0)
                AssessmentYear = DateTime.Now.Year;
        }
        
        [DataMember]
        public String Author
        {
            get => _author;
            set => Set(ref _author, value);
        }

        [DataMember]
        public String Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        [DataMember]
        public Int32 AssessmentYear
        {
            get => _assessmentYear;
            set => Set(ref _assessmentYear, value);
        }

        [DataMember]
        public String Notes
        {
            get => _notes;
            set => Set(ref _notes, value);
        }

        [DataMember]
        public DateTime Created
        {
            get => _created;
            set => Set(ref _created, value);
        }

        [DataMember]
        public Guid UserId
        {
            get => _userId;
            set => Set(ref _userId, value);
        }

        [DataMember]
        public DateTime Modified { get; set; }
        
        [DataMember]
        public Int32 Wkid
        {
            get => _wkid;
            set => Set(ref _wkid, value);
        }
        
        [DataMember]
        public Guid Id { get; private set; }
    }
}