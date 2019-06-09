using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CsvHelper;

namespace FhiModel.Governance
{
    public class Import
    {
        public static List<List<Question>> Read(string filename, string userColumn, string startColumn, string lastQuestion)
        {
            var rv = new List<List<Question>>();
            using (var stream = new FileStream(filename, FileMode.Open))
            {
                using (var reader = new CsvReader(new StreamReader(stream, Encoding.UTF8)))
                {
                    var questions = new Dictionary<int, String>();
                    reader.Read();
                    // get the questions
                    var questionStartIndex = ColumnToIndex(startColumn);
                    var column = 0;
                    while (reader.TryGetField<String>(column, out var field))
                    {
                        if (column++ < questionStartIndex) continue;
                        questions.Add(column, field);
                    }
                    
                    // read the answers
                    var answers = new Dictionary<String, List<AnswerItem>>();
                    while (reader.Read())
                    {
                        var user = reader.GetField(ColumnToIndex(userColumn));
                        answers[user] = new List<AnswerItem>();
                        column = 0;
                        while (reader.TryGetField<String>(column, out var field))
                        {
                            if (column++ < questionStartIndex) continue;
                            answers[user].Add(new AnswerItem {Column = column, Answer = field});
                        }
                    }
                    
                    // we have all the questions and the answers.
                    // assume that the questions are grouped with "Please provide" separating them
                    
                    List<Question> currentTranche = null;
                    foreach (var fileQuestion in questions)
                    {
                        if (currentTranche == null)
                            currentTranche = new List<Question>();
                        var question = new Question
                        {
                            Name = fileQuestion.Value,
                            Answers =  new List<Answer>()
                        };
                        foreach (var fileAnswer in answers)
                        {
                            foreach (var ua in fileAnswer.Value)
                            {
                                if (ua.Column != fileQuestion.Key) continue;
                                var a = new Answer {User = fileAnswer.Key};
                                if (int.TryParse(ua.Answer, out var v))
                                    a.Value = v;
                                else
                                    a.Comment = ua.Answer;
                                question.Answers.Add(a);
                            }
                        }
                        
                        currentTranche.Add(question);
                        if (!fileQuestion.Value.StartsWith(lastQuestion)) continue;
                        
                        rv.Add(currentTranche);
                        currentTranche = null;
                    }
                }
            }

            return rv;
        }

        private static int ColumnToIndex(string column)
        {
            if (column.Length > 1)
                throw new ArgumentException($"can't convert '{column}' yet.");
            return column.ToUpperInvariant()[0] - 'A';
        }

        private class AnswerItem
        {
            public int Column { get; set; }
            public String Answer { get; set; }

            public override String ToString()
            {
                return $"{Column} : {Answer}";
            }
        }
    }
}