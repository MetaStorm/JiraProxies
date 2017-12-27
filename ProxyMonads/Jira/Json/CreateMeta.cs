using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {

  public class Rootobject {
    public string expand { get; set; }
    public Project[] projects { get; set; }
  }

  public class Issuetype {
    public string self { get; set; }
    public string id { get; set; }
    public string description { get; set; }
    public string iconUrl { get; set; }
    public string name { get; set; }
    public bool subtask { get; set; }
    public string expand { get; set; }
    public Fields fields { get; set; }
  }

  public class Fields {
    public Issuetype1 issuetype { get; set; }
    public Components components { get; set; }
    public Description description { get; set; }
    public Project1 project { get; set; }
    public Fixversions fixVersions { get; set; }
    public Customfield_10012 customfield_10012 { get; set; }
    public Customfield_10013 customfield_10013 { get; set; }
    public Customfield_10112 customfield_10112 { get; set; }
    public Customfield_10113 customfield_10113 { get; set; }
    public Customfield_10114 customfield_10114 { get; set; }
    public Timetracking timetracking { get; set; }
    public Security security { get; set; }
    public Attachment attachment { get; set; }
    public Summary summary { get; set; }
    public Customfield_10180 customfield_10180 { get; set; }
    public Reporter reporter { get; set; }
    public Customfield_10241 customfield_10241 { get; set; }
    public Priority priority { get; set; }
    public Customfield_10122 customfield_10122 { get; set; }
    public Customfield_10115 customfield_10115 { get; set; }
    public Customfield_10116 customfield_10116 { get; set; }
    public Customfield_10018 customfield_10018 { get; set; }
    public Customfield_10117 customfield_10117 { get; set; }
    public Versions versions { get; set; }
    public Duedate duedate { get; set; }
    public Assignee assignee { get; set; }
    public Parent parent { get; set; }
    public Customfield_11045 customfield_11045 { get; set; }
    public Customfield_11750 customfield_11750 { get; set; }
    public Customfield_10843 customfield_10843 { get; set; }
    public Customfield_10142 customfield_10142 { get; set; }
    public Customfield_10143 customfield_10143 { get; set; }
    public JiraEnvironment environment { get; set; }
    public Labels labels { get; set; }
    public Customfield_11140 customfield_11140 { get; set; }
    public Customfield_10150 customfield_10150 { get; set; }
    public Customfield_11142 customfield_11142 { get; set; }
    public Customfield_11143 customfield_11143 { get; set; }
    public Customfield_10144 customfield_10144 { get; set; }
    public Customfield_11145 customfield_11145 { get; set; }
    public Customfield_10145 customfield_10145 { get; set; }
    public Customfield_11146 customfield_11146 { get; set; }
    public Customfield_10020 customfield_10020 { get; set; }
    public Customfield_10550 customfield_10550 { get; set; }
    public Customfield_10220 customfield_10220 { get; set; }
    public Customfield_10221 customfield_10221 { get; set; }
    public Customfield_10222 customfield_10222 { get; set; }
    public Customfield_10223 customfield_10223 { get; set; }
    public Customfield_10019 customfield_10019 { get; set; }
    public Customfield_12040 customfield_12040 { get; set; }
    public Customfield_11044 customfield_11044 { get; set; }
    public Customfield_11541 customfield_11541 { get; set; }
    public Customfield_11742 customfield_11742 { get; set; }
    public Customfield_11741 customfield_11741 { get; set; }
    public Customfield_10842 customfield_10842 { get; set; }
    public Customfield_11743 customfield_11743 { get; set; }
    public Customfield_11749 customfield_11749 { get; set; }
    public Customfield_11744 customfield_11744 { get; set; }
    public Customfield_11746 customfield_11746 { get; set; }
    public Customfield_11745 customfield_11745 { get; set; }
    public Customfield_11748 customfield_11748 { get; set; }
    public Customfield_11747 customfield_11747 { get; set; }
    public Customfield_11040 customfield_11040 { get; set; }
    public Customfield_11041 customfield_11041 { get; set; }
    public Customfield_11042 customfield_11042 { get; set; }
    public Customfield_11043 customfield_11043 { get; set; }
    public Customfield_10132 customfield_10132 { get; set; }
    public Customfield_10740 customfield_10740 { get; set; }
    public Customfield_10848 customfield_10848 { get; set; }
    public Customfield_10160 customfield_10160 { get; set; }
    public Customfield_11840 customfield_11840 { get; set; }
    public Customfield_11644 customfield_11644 { get; set; }
    public Customfield_11141 customfield_11141 { get; set; }
    public Customfield_11447 customfield_11447 { get; set; }
    public Customfield_11250 customfield_11250 { get; set; }
    public Customfield_11441 customfield_11441 { get; set; }
    public Customfield_11444 customfield_11444 { get; set; }
    public Issuelinks issuelinks { get; set; }
    public Resolution resolution { get; set; }
    public Customfield_11940 customfield_11940 { get; set; }
    public Customfield_11249 customfield_11249 { get; set; }
    public Customfield_10849 customfield_10849 { get; set; }
    public Customfield_10850 customfield_10850 { get; set; }
    public Customfield_10851 customfield_10851 { get; set; }
    public Customfield_11240 customfield_11240 { get; set; }
    public Customfield_11251 customfield_11251 { get; set; }
    public Customfield_11243 customfield_11243 { get; set; }
    public Customfield_11643 customfield_11643 { get; set; }
  }

  public class Issuetype1 {
    public bool required { get; set; }
    public Schema schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public object[] operations { get; set; }
    public Allowedvalue[] allowedValues { get; set; }
  }

  public class Schema {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Allowedvalue {
    public string self { get; set; }
    public string id { get; set; }
    public string description { get; set; }
    public string iconUrl { get; set; }
    public string name { get; set; }
    public bool subtask { get; set; }
    public int avatarId { get; set; }
  }

  public class Components {
    public bool required { get; set; }
    public Schema1 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue1[] allowedValues { get; set; }
  }

  public class Schema1 {
    public string type { get; set; }
    public string items { get; set; }
    public string system { get; set; }
  }

  public class Allowedvalue1 {
    public string self { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
  }

  public class Description {
    public bool required { get; set; }
    public Schema2 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema2 {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Project1 {
    public bool required { get; set; }
    public Schema3 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue2[] allowedValues { get; set; }
  }

  public class Schema3 {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Allowedvalue2 {
    public string self { get; set; }
    public string id { get; set; }
    public string key { get; set; }
    public string name { get; set; }
    public Avatarurls1 avatarUrls { get; set; }
    public Projectcategory projectCategory { get; set; }
  }

  public class Fixversions {
    public bool required { get; set; }
    public Schema4 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue3[] allowedValues { get; set; }
  }

  public class Schema4 {
    public string type { get; set; }
    public string items { get; set; }
    public string system { get; set; }
  }

  public class Allowedvalue3 {
    public string self { get; set; }
    public string id { get; set; }
    public string description { get; set; }
    public string name { get; set; }
    public bool archived { get; set; }
    public bool released { get; set; }
    public string releaseDate { get; set; }
    public string userReleaseDate { get; set; }
    public int projectId { get; set; }
    public bool overdue { get; set; }
  }

  public class Customfield_10012 {
    public bool required { get; set; }
    public Schema5 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema5 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10013 {
    public bool required { get; set; }
    public Schema6 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema6 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10112 {
    public bool required { get; set; }
    public Schema7 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema7 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10113 {
    public bool required { get; set; }
    public Schema8 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema8 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10114 {
    public bool required { get; set; }
    public Schema9 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue4[] allowedValues { get; set; }
  }

  public class Schema9 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue4 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Schema10 {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Security {
    public bool required { get; set; }
    public Schema11 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue5[] allowedValues { get; set; }
  }

  public class Schema11 {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Allowedvalue5 {
    public string self { get; set; }
    public string id { get; set; }
    public string description { get; set; }
    public string name { get; set; }
  }


  public class Schema12 {
    public string type { get; set; }
    public string items { get; set; }
    public string system { get; set; }
  }

  public class Summary {
    public bool required { get; set; }
    public Schema13 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema13 {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Customfield_10180 {
    public bool required { get; set; }
    public Schema14 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema14 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Reporter {
    public bool required { get; set; }
    public Schema15 schema { get; set; }
    public string name { get; set; }
    public string autoCompleteUrl { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema15 {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Customfield_10241 {
    public bool required { get; set; }
    public Schema16 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue6[] allowedValues { get; set; }
  }

  public class Schema16 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue6 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Priority {
    public bool required { get; set; }
    public Schema17 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue7[] allowedValues { get; set; }
  }

  public class Schema17 {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Allowedvalue7 {
    public string self { get; set; }
    public string iconUrl { get; set; }
    public string name { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10122 {
    public bool required { get; set; }
    public Schema18 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema18 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10115 {
    public bool required { get; set; }
    public Schema19 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema19 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10116 {
    public bool required { get; set; }
    public Schema20 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema20 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10018 {
    public bool required { get; set; }
    public Schema21 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue8[] allowedValues { get; set; }
  }

  public class Schema21 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue8 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10117 {
    public bool required { get; set; }
    public Schema22 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema22 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Versions {
    public bool required { get; set; }
    public Schema23 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue9[] allowedValues { get; set; }
  }

  public class Schema23 {
    public string type { get; set; }
    public string items { get; set; }
    public string system { get; set; }
  }

  public class Allowedvalue9 {
    public string self { get; set; }
    public string id { get; set; }
    public string description { get; set; }
    public string name { get; set; }
    public bool archived { get; set; }
    public bool released { get; set; }
    public string releaseDate { get; set; }
    public string userReleaseDate { get; set; }
    public int projectId { get; set; }
    public bool overdue { get; set; }
  }

  public class Duedate {
    public bool required { get; set; }
    public Schema24 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema24 {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Assignee {
    public bool required { get; set; }
    public Schema25 schema { get; set; }
    public string name { get; set; }
    public string autoCompleteUrl { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema25 {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Parent {
    public bool required { get; set; }
    public Schema26 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema26 {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Customfield_11045 {
    public bool required { get; set; }
    public Schema27 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema27 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11750 {
    public bool required { get; set; }
    public Schema28 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue10[] allowedValues { get; set; }
  }

  public class Schema28 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue10 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10843 {
    public bool required { get; set; }
    public Schema29 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema29 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10142 {
    public bool required { get; set; }
    public Schema30 schema { get; set; }
    public string name { get; set; }
    public string autoCompleteUrl { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema30 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10143 {
    public bool required { get; set; }
    public Schema31 schema { get; set; }
    public string name { get; set; }
    public string autoCompleteUrl { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema31 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class JiraEnvironment {
    public bool required { get; set; }
    public Schema32 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema32 {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Labels {
    public bool required { get; set; }
    public Schema33 schema { get; set; }
    public string name { get; set; }
    public string autoCompleteUrl { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema33 {
    public string type { get; set; }
    public string items { get; set; }
    public string system { get; set; }
  }

  public class Customfield_11140 {
    public bool required { get; set; }
    public Schema34 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema34 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10150 {
    public bool required { get; set; }
    public Schema35 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema35 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11142 {
    public bool required { get; set; }
    public Schema36 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue11[] allowedValues { get; set; }
  }

  public class Schema36 {
    public string type { get; set; }
    public string items { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue11 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_11143 {
    public bool required { get; set; }
    public Schema37 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue12[] allowedValues { get; set; }
  }

  public class Schema37 {
    public string type { get; set; }
    public string items { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue12 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10144 {
    public bool required { get; set; }
    public Schema38 schema { get; set; }
    public string name { get; set; }
    public string autoCompleteUrl { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema38 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11145 {
    public bool required { get; set; }
    public Schema39 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue13[] allowedValues { get; set; }
  }

  public class Schema39 {
    public string type { get; set; }
    public string items { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue13 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10145 {
    public bool required { get; set; }
    public Schema40 schema { get; set; }
    public string name { get; set; }
    public string autoCompleteUrl { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema40 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11146 {
    public bool required { get; set; }
    public Schema41 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue14[] allowedValues { get; set; }
  }

  public class Schema41 {
    public string type { get; set; }
    public string items { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue14 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10020 {
    public bool required { get; set; }
    public Schema42 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema42 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10550 {
    public bool required { get; set; }
    public Schema43 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue15[] allowedValues { get; set; }
  }

  public class Schema43 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue15 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10220 {
    public bool required { get; set; }
    public Schema44 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue16[] allowedValues { get; set; }
  }

  public class Schema44 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue16 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10221 {
    public bool required { get; set; }
    public Schema45 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema45 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10222 {
    public bool required { get; set; }
    public Schema46 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue17[] allowedValues { get; set; }
  }

  public class Schema46 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue17 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10223 {
    public bool required { get; set; }
    public Schema47 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue18[] allowedValues { get; set; }
  }

  public class Schema47 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue18 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10019 {
    public bool required { get; set; }
    public Schema48 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema48 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_12040 {
    public bool required { get; set; }
    public Schema49 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema49 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11044 {
    public bool required { get; set; }
    public Schema50 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema50 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11541 {
    public bool required { get; set; }
    public Schema51 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema51 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11742 {
    public bool required { get; set; }
    public Schema52 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema52 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11741 {
    public bool required { get; set; }
    public Schema53 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema53 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10842 {
    public bool required { get; set; }
    public Schema54 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema54 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11743 {
    public bool required { get; set; }
    public Schema55 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema55 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11749 {
    public bool required { get; set; }
    public Schema56 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema56 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11744 {
    public bool required { get; set; }
    public Schema57 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema57 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11746 {
    public bool required { get; set; }
    public Schema58 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema58 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11745 {
    public bool required { get; set; }
    public Schema59 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema59 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11748 {
    public bool required { get; set; }
    public Schema60 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema60 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11747 {
    public bool required { get; set; }
    public Schema61 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema61 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11040 {
    public bool required { get; set; }
    public Schema62 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue19[] allowedValues { get; set; }
  }

  public class Schema62 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue19 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_11041 {
    public bool required { get; set; }
    public Schema63 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue20[] allowedValues { get; set; }
  }

  public class Schema63 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue20 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_11042 {
    public bool required { get; set; }
    public Schema64 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue21[] allowedValues { get; set; }
  }

  public class Schema64 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue21 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_11043 {
    public bool required { get; set; }
    public Schema65 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema65 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10132 {
    public bool required { get; set; }
    public Schema66 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue22[] allowedValues { get; set; }
  }

  public class Schema66 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue22 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10740 {
    public bool required { get; set; }
    public Schema67 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue23[] allowedValues { get; set; }
  }

  public class Schema67 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue23 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
    public Child[] children { get; set; }
  }

  public class Child {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10848 {
    public bool required { get; set; }
    public Schema68 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue24[] allowedValues { get; set; }
  }

  public class Schema68 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue24 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10160 {
    public bool required { get; set; }
    public Schema69 schema { get; set; }
    public string name { get; set; }
    public string autoCompleteUrl { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema69 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11840 {
    public bool required { get; set; }
    public Schema70 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema70 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11644 {
    public bool required { get; set; }
    public Schema71 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema71 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11141 {
    public bool required { get; set; }
    public Schema72 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema72 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11447 {
    public bool required { get; set; }
    public Schema73 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema73 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11250 {
    public bool required { get; set; }
    public Schema74 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema74 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11441 {
    public bool required { get; set; }
    public Schema75 schema { get; set; }
    public string name { get; set; }
    public string autoCompleteUrl { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema75 {
    public string type { get; set; }
    public string items { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11444 {
    public bool required { get; set; }
    public Schema76 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue25[] allowedValues { get; set; }
  }

  public class Schema76 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue25 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Issuelinks {
    public bool required { get; set; }
    public Schema77 schema { get; set; }
    public string name { get; set; }
    public string autoCompleteUrl { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema77 {
    public string type { get; set; }
    public string items { get; set; }
    public string system { get; set; }
  }

  public class Resolution {
    public bool required { get; set; }
    public Schema78 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue26[] allowedValues { get; set; }
  }

  public class Schema78 {
    public string type { get; set; }
    public string system { get; set; }
  }

  public class Allowedvalue26 {
    public string self { get; set; }
    public string name { get; set; }
    public string id { get; set; }
  }

  public class Customfield_11940 {
    public bool required { get; set; }
    public Schema79 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue27[] allowedValues { get; set; }
  }

  public class Schema79 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue27 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_11249 {
    public bool required { get; set; }
    public Schema80 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
    public Allowedvalue28[] allowedValues { get; set; }
  }

  public class Schema80 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Allowedvalue28 {
    public string self { get; set; }
    public string value { get; set; }
    public string id { get; set; }
  }

  public class Customfield_10849 {
    public bool required { get; set; }
    public Schema81 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema81 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10850 {
    public bool required { get; set; }
    public Schema82 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema82 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_10851 {
    public bool required { get; set; }
    public Schema83 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema83 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11240 {
    public bool required { get; set; }
    public Schema84 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema84 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11251 {
    public bool required { get; set; }
    public Schema85 schema { get; set; }
    public string name { get; set; }
    public string autoCompleteUrl { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema85 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11243 {
    public bool required { get; set; }
    public Schema86 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema86 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }

  public class Customfield_11643 {
    public bool required { get; set; }
    public Schema87 schema { get; set; }
    public string name { get; set; }
    public bool hasDefaultValue { get; set; }
    public string[] operations { get; set; }
  }

  public class Schema87 {
    public string type { get; set; }
    public string custom { get; set; }
    public int customId { get; set; }
  }


}
