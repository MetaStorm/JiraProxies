using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Wcf.ProxyMonads;
using Jira.Json;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using CommonExtensions;
using System.Dynamic;
using System.Runtime.ExceptionServices;
using System.Net;
using static Wcf.ProxyMonads.RestExtenssions;
using static Jira.Json.IssueClasses;
using WLS = System.Tuple<System.DateTime, System.TimeSpan, Jira.Rest.TicketTransitionHistory, Jira.Json.IssueClasses.Assignee>;
using System.Runtime.CompilerServices;

namespace Jira {
  public static partial class Rest {

  }
}
