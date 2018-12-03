using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wcf.ProxyMonads;
using CommonExtensions;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Diagnostics;
using Jira;
using Jira.Json;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Net;

namespace Proxies.ExternalTests {
  [TestClass]
  public class WorkflowTest {
    static int[] _workflowSchemeIds;
    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context) {
      _workflowSchemeIds = new RestMonad().GetWorlflowSchemeIdsAsync().GetAwaiter().GetResult().Value.Select(int.Parse).ToArray();
      RestConfiger.WorkflowSchemaIdsProvider = () => _workflowSchemeIds;
      RestConfiger.ProjectIssueTypeWorkflowProvider = RestConfiger.GetProjectIssueTypeWorkflowAsync;
    }
    [TestMethod]
    public async Task GetWorkflows() {
      var workflows = (await RestMonad.Empty().GetWorkflows()).Value;
      Console.WriteLine(workflows.ToJson());
    }
    [TestMethod]
    public async Task GetWorkflowSchemas() {
      var itws = await RestConfiger.IssueTypeWorkflows;
      Assert.IsTrue(itws.Any(), $"{nameof(RestConfiger.IssueTypeWorkflows)} is empty");
      Console.WriteLine(itws.ToJson());
      var project =  "HLP";
      var issueType =  "Password Reset";
      var workflow = "WorkFlowHD";
      var wf = await RestConfiger.ProjectIssueTypeWorkflow(project, issueType);
      Assert.AreEqual(workflow, wf);
      await ExceptionAssert.Propagates<Exception>(() => RestConfiger.ProjectIssueTypeWorkflow(project, "Task"), exc => {
        Assert.IsTrue(exc.Message.Contains("issueType = Task"));
      });
      await ExceptionAssert.Propagates<PassagerException>(() => RestConfiger.ProjectIssueTypeWorkflow("XXXXXXX", "YYY"), exc => {
        Assert.IsTrue(exc.Message.Contains("project = XXXXXXX"));
      });
      Assert.IsTrue((await RestConfiger.ResetIssueTypeWorkflows()).Any());
    }
    [TestMethod]
    [TestCategory("Manual")]
    public async Task GetWorkflowSchemasProvider_M() {
      Assert.Inconclusive();
      RestConfiger.WorkflowSchemaIdsProvider = () => new[] { 10093 };
      Console.WriteLine((await RestConfiger.IssueTypeWorkflows).ToJson());
      var wf = await RestConfiger.IssueTypeWorkflows;
      Assert.AreEqual(4, wf.Count());
      Assert.AreEqual("WorkFlowCNG", wf.First().Single());
    }
    [TestMethod]
    public async Task GetStatuses() {
      var statuses = (await RestMonad.Empty().GetStatuses()).Value;
      Console.WriteLine(statuses.ToJson());
      Assert.IsTrue(statuses.Length > 0);
    }
    [TestMethod]
    [TestCategory("Manual")]
    public async Task ProjectIssueTypeWorkflow() {
      Assert.Inconclusive();
      var rt = await RestConfiger.RunTestAsync(null);
      var pitw = ((IEnumerable<object>)((IDictionary<string, object>)((IDictionary<string, object>)rt)["Jira.RestConfiger"])["pitw"]).ToArray();
      Console.WriteLine(rt.ToJson());
    }

    [TestMethod]
    [TestCategory("Manual")]
    public async Task DeleteStatus_M() {
      Assert.Inconclusive();
      var js = await Rest.DeleteStatusAsync(11492);
      Console.WriteLine(js.ToJson());
    }
    [TestMethod]
    public async Task PostStatusNew() {
      var statusName = "Test Status " + CommonExtensions.Helpers.RandomStringUpper(4);
      var js = await Rest.PostStatusAsync(statusName, "Delete Me");
      Console.WriteLine(new { statusName, response = js.ToJson() });
    }

    //
    [TestMethod]
    public async Task PostStatusOld() {
      await TestAlreadyExists(Rest.PostStatusAsync("Step1", "Delete Me"));
    }
    //
    [TestMethod]
    public async Task PostStatusLong() {
      var status = "Review all client documents and IES transactions to confirm no contact has been made with client";
      try {
        await Rest.PostStatusAsync(status, "Delete Me");
      } catch(Exception exc) {
        if(exc.Message.Contains("too long"))
          return;
        throw;
      }
      Assert.Fail();
    }
    [TestMethod]
    [TestCategory("Manual")]
    public async Task PostStatus60_M() {
      Assert.Inconclusive();
      var status = "Review all client documents and IES transactions to confirm no contact has been made with client";
      await Rest.PostStatusAsync(status.Substring(0, 60), "Delete Me");
    }

    [TestMethod]
    public async Task PostWorkflow() {
      await TestAlreadyExists(Rest.PostWorkflowAsync("jira", "Delete Me", ""));
    }
    [TestMethod]
    [TestCategory("Manual")]
    public async Task DeleteWorkflow_M() {
      Assert.Inconclusive();
      var workflows = new[] {"WorkFlowHD",
"WorkFlowCNG",
"WorkFlowProjects",
"WorkFlowRQ",
"classic default workflow",
"IPB - MIAMI - Workflow - CLF",
"IPB - MIAMI - Workflow - Statement Quality Check v2",
"IPB - MIAMI - Workflow - Wire Notification - Multiple Options v4",
"IPB - MIAMI - Workflow - Wire Notification - Unidirectional",
"Copy of IPB - MIAMI - Workflow - Statement Quality Check v2",
"SR: 4MC Account Opening Migration with non approved Mutual or Hedge Funds Workflow",
"SR: E-Signature - RMs based in Miami - Private Banking (BIEI+IES) Workflow",
"SR: Regional Offices - Private Banking (BIEI+IES) NEW! - High Risk Workflow",
"SR: RMs based in Miami - Private Banking (BIEI+IES) NEW! - High Risk Workflow",
"SR: ZZZ_4373 Incoming Assets (Discontinued- open to see replacement) Workflow",
"SR: ZZZ_Incoming Assets - Mutual and/or Hedge Funds Only (Discontinued- open to see replacement) Workflow",
"SR: ZZZ_Internal Transfer Different Ownership - JT2/JT4 Related accounts (Discontinued- open to see replacement) Workflow",
"SR: ZZZ_Internal Transfer Different Ownership (Discontinued- open to see replacement) Workflow",
"SR: ZZZ_Internal Transfer Same Ownership - JT2/JT4 related accounts (Same account names and signers) (Discontinued- open to see replacement) Workflow",
"SR: ZZZ_Internal Trasnfer Same Ownership (Same Account names and signers) (Discontinued- open to see replacement) Workflow",
"SR: ZZZ_Outgoing Asset - Third Party Different name (Discontinued- open to see replacement) Workflow",
"SR: ZZZ_Outgoing Assets - JT2 Accounts (Discontinued- open to see replacement) Workflow",
"SR: ZZZ_Outgoing Assets - JT2/JT4 related accounts (Discontinued- open to see replacement) Workflow",
"SR: ZZZ_Outgoing Assets: Mutual and Hedge Funds Only (Discontinued- open to see replacement) Workflow",
"SR: ZZZ_Outgoing Assets: Outgoing Assets: Bonds/Equities (Discontinued- open to see replacement) Workflow",
"SR: ZZZ_Outgoing Transfer 4373 (Discontinued- open to see replacement) Workflow",
"SR: Permanent Unblock: Corporations, PICs, Trusts and Other Entities Workflow",
"SR: Reason 02: Block to close / Closing <200K / CLOSING PJCT HR<1MM Workflow",
"SR: Reason 08: AML MONITOR IN. DEP / AML TO CLOSE / B. SHARE CERT EXP / F-UP ARTICLES - BVI / KYC EXP OVER 1 YR / NEG MEDIA Workflow",
"SR: Reason 09: CHANGE OF OWNERSHIP / DORMANT / MIGRATE TO MIA / MISSING DOCS / NOT SUBJ TO MIGRAT. / ONLINE BK ENROLLMENT / RETURNED MAIL Workflow",
"AM: Task Management Workflow",
"SR: Account Grouping Workflow",
"SR: Bearer Shares Certificate of Beneficial Owner Update Workflow",
"SR: BII Options Agreement Workflow",
"SR: Change of Address or Phones: Callback Required Workflow",
"SR: Change of Address or Phones: Callback Required - PWM Workflow",
"SR: Change of Address or Phones: No Callback Required Workflow",
"SR: Change of Address or Phones: No Callback Required - PWM Workflow",
"SR: Change of Banker Workflow",
"SR: Change of mail option to ELECTRONIC MAIL Workflow",
"SR: Change of mail option to REGULAR MAIL: Callback Required Workflow",
"SR: Change of mail option to REGULAR MAIL: No Callback Required Workflow",
"SR: Change of mail option to SPECIAL HANDLING Workflow",
"SR: Client Investment Profile Bank Only Workflow",
"SR: Client Investment Update Bank and Broker Dealer Workflow",
"SR: Closing Project Letter: AUM below $200k Workflow",
"SR: Closing Project Letter: High Risk Clients Workflow",
"SR: Customer Information update Workflow",
"SR: Customer Information Update - PWM Workflow",
"SR: Customer Information Update (Fiduciary Structures - PB) Workflow",
"SR: Customer Information Update (Fiduciary Structures - PWM) Workflow",
"SR: Email Update Workflow",
"SR: Email Update - Callback Required Workflow",
"SR: Hedge Fund Letter Workflow",
"SR: Hold or Returned Mail Delivery: Address on records Workflow",
"SR: Hold or Returned Mail Delivery: Callback required Workflow",
"SR: IAAA Corporate Action Workflow",
"SR: Internet Upload Workflow",
"SR: Investment Info Addendum - BANKERS IN MIAMI Workflow",
"SR: Investment Info Addendum - REGIONAL OFFICES Workflow",
"SR: Letter for certificate expired Workflow",
"SR: Limited POA - Itau Employee and/or IFO Workflow",
"SR: Mailing Option and statement frequency changes Workflow",
"SR: Open new DDA Workflow",
"SR: Options and Master Agreement and Others agreements Workflow",
"SR: Signers update (add or remove signers) Workflow",
"SR: Signers update (add or remove signers) - High Risk Workflow",
"SR: Submission of Originals for Account Opening Application Workflow",
"SR: Suitability Letter - sent to Client Workflow",
"SR: Test Key Set Up Workflow",
"SR: US Indicia Cure Workflow",
"SR: US Indicia Report (Change of Circumstances) Workflow",
"SR: W8 Update Workflow",
"SR: W8 Update - PWM Workflow",
"SR: W8 Update/Reverse Tax Withholding (BANK LINKED) Workflow",
"SR: W8 Update/Reverse Tax Withholding (BANK) Workflow",
"SR: ZZZ - Stanting Instrucions - No callback required (fees only) (DO NOT USE) Workflow",
"SR: ZZZ- Execution standing Instruction AMEX - Bahamas (DO NOT USE) Workflow",
"SR: ZZZ - Mutual and Hedge Funds Exception (Discontinued) DO NOT USE Workflow",
"Test With Back",
"SR: Change of Address or Phones: No Callback Required - PWM--BIEI Workflow",
"SR: Change of mail option to REGULAR MAIL: Callback Required--BIEI Workflow",
"SR: Change of mail option to REGULAR MAIL: Callback Required--IES Workflow",
"SR: Change of mail option to REGULAR MAIL: No Callback Required--BIEI Workflow",
"SR: Change of mail option to REGULAR MAIL: No Callback Required--IES Workflow",
"SR: Changes of Address or phone – No Callback Required - PWM--IES Workflow",
"SR: Customer Information Update (Fiduciary Structures - PB)--BIEI Workflow",
"SR: Customer Information Update (Fiduciary Structures - PWM)--BIEI Workflow",
"SR: Customer Information Update (Fiduciary Structures - PWM)--IES Workflow",
"SR: Forms/ Documents Preparation - to be used by Miami Sales Team only--IES Workflow",
"SR: Submission of Originals for Account Opening Application--BIEI Workflow",
"SR: 4MC Account Opening Migration with non approved Mutual or Hedge Funds--BIEI Workflow",
"SR: E-Signature - RMs based in Miami - Private Banking (BIEI+IES)--BIEI Workflow",
"SR: Regional Offices - Private Banking (BIEI+IES) NEW! - High Risk--BIEI Workflow",
"SR: RMs based in Miami - Private Banking (BIEI+IES) NEW! - High Risk--BIEI Workflow",
"SR: Incoming Assets - JT3 / JT5 Bonds and/or Equities (Stand Alone Account)--IES Workflow",
"SR: Incoming Assets - JT3 / JT5 Mutual Funds (Stand Alone Account)--IES Workflow",
"SR: Incoming Assets - JT3 Mutual Funds (Stand Alone Account)--IES Workflow",
"SR: Internal Transfer - JT3 / JT5 Bonds and/or Equities (Stand Alone Account)--IES Workflow",
"SR: Internal Transfer - JT3 / JT5 Mutual Funds (Stand Alone Account)--IES Workflow",
"SR: Internal Transfer - JT3 Mutual Funds (Stand Alone Account)--IES Workflow",
"SR: Outgoing Assets - JT3 Mutual Funds (Stand Alone Account)--IES Workflow",
"SR: Outgoing Assets - JT3/ JT5 Bonds and/or Equities (Stand Alone Account)--IES Workflow",
"SR: Outgoing Assets - JT3/ JT5 Mutual Funds Only (Stand Alone Account)--IES Workflow",
"SR: ZZZ_4373 Incoming Assets (Discontinued- open to see replacement)--BIEI Workflow",
"SR: ZZZ_Incoming Assets - Mutual and/or Hedge Funds Only (Discontinued- open to see replacement)--BIEI Workflow",
"SR: ZZZ_Internal Transfer Different Ownership - JT2 related accounts (Discontinued- open to see replacement)--IES Workflow",
"SR: ZZZ_Internal Transfer Different Ownership - JT2/JT4 Related accounts (Discontinued- open to see replacement)--BIEI Workflow",
"SR: ZZZ_Internal Transfer Different Ownership (Discontinued- open to see replacement)--BIEI Workflow",
"SR: ZZZ_Internal Transfer Same Ownership - JT2 related accounts (Discontinued- open to see replacement)--IES Workflow",
"SR: ZZZ_Internal Transfer Same Ownership - JT2/JT4 related accounts (Same account names and signers) (Discontinued- open to see replacement)--BIEI Workflow",
"SR: ZZZ_Internal Transfer Same Ownership (Discontinued- open to see replacement)--IES Workflow",
"SR: ZZZ_Internal Trasnfer Same Ownership (Same Account names and signers) (Discontinued- open to see replacement)--BIEI Workflow",
"SR: ZZZ_Outgoing Asset - Third Party Different name (Discontinued- open to see replacement)--BIEI Workflow",
"SR: ZZZ_Outgoing Assets - JT2 Accounts (Discontinued- open to see replacement)--BIEI Workflow",
"SR: ZZZ_Outgoing Assets - JT2/JT4 related accounts (Discontinued- open to see replacement)--BIEI Workflow",
"SR: ZZZ_Outgoing Assets - Third party different name (Discontinued- open to see replacement)--IES Workflow",
"SR: ZZZ_Outgoing Assets: Mutual and Hedge Funds Only (Discontinued- open to see replacement)--BIEI Workflow",
"SR: ZZZ_Outgoing Assets: Outgoing Assets: Bonds/Equities (Discontinued- open to see replacement)--BIEI Workflow",
"SR: ZZZ_Outgoing Transfer 4373 (Discontinued- open to see replacement)--BIEI Workflow",
"SR: Time Deposits / Placements--BIEI Workflow",
"SR: Standing Instruction Payment Execution - Managed Account--BIEI Workflow",
"SR: ZZZ - Standing Instructions Set-up - JT5 Accounts (DO NOT USE)--IES Workflow",
"SR: ZZZ - Stanting Instrucions - No callback required (fees only) (DO NOT USE)--BIEI Workflow",
"SR: ZZZ- Execution standing Instruction AMEX - Bahamas (DO NOT USE)--BIEI Workflow",
"SR: ZZZ- Standing Instructions set-up (Discontinued) DO NOT USE--BIEI Workflow",
"SR: Responses for voluntary corporate actions - Recorded Line--BIEI Workflow",
"SR: Responses for voluntary corporate actions - Recorded Line--IES Workflow",
"SR: ZZZ_ AMEX Statement copy request (Discontinued- Do Not Use)--BIEI Workflow",
"SR: Permanent Unblock: Corporations, PICs, Trusts and Other Entities--BIEI Workflow",
"SR: Permanent Unblock: Corporations, PICs, Trusts and Other Entities--IES Workflow",
"SR: Permanent Unblock: Transfer on Death Accounts (all types)--BIEI Workflow",
"SR: Permanent Unblock: Transfer on Death Accounts (all types)--IES Workflow",
"SR: aive ADM Fees for accounts with defaulted securities only--IES Workflow",
"SR: Waive ADM Fees for accounts with defaulted securities only--BIEBT Workflow",
"SR: Waive ADM Fees for accounts with defaulted securities only--BIEI Workflow",
"SR: ZZZ - Mutual and Hedge Funds Exception (Discontinued) DO NOT USE--BIEI Workflow",
"SR: ZZZ - Mutual and Hedge Funds Exception (Discontinued) DO NOT USE--IES Workflow",
"SR: Gain and losses above USD 5,000 (No client related)--INTERNAL Workflow",
"SR: ZZZ_Set up Fees_(Discontinued- Open to see replacement) DO NOT USE--BIEI Workflow",
"SR: ZZZ_Update IFO POA/Signers_(Discontinued- Open to see replacement) DO NOT USE--BIEI Workflow",
"SR: Chile and MCC requests--INTERNAL Workflow",
"SR: Compliance Approval Miami Open 2017--INTERNAL Workflow",
"SR: Credit Annual Reviews--BIEI Workflow",
"SR: Itau Forms - BII--INTERNAL Workflow",
"SR: Itau Forms - IIS--INTERNAL Workflow",
"SR: MCC Issues--INTERNAL Workflow",
"SR: Renewal of loans with standing instructions which is done via e-mail--BIEI Workflow",
"SR: Request Changes on the Service Request--INTERNAL Workflow",
"SR: Signature Verification for Credit Documents--BIEI Workflow",
"SR: VISA approvals for wires, billpayer, Amex, Custody, fees, etc.--BIEI Workflow",
"SR: Link or Unlink Accounts--BIEI Workflow",
"SR: Net Exchange Pro set-up--IES Workflow",
"SR: Net Exchange Pro set-up - PWM--IES Workflow",
"SR: Online Banking set-up--BIEI Workflow",
"SR: Online Banking set-up - PWM--BIEI Workflow",
"SR: Request to Reset Password--BIEI Workflow",
"SR: Request to Reset Password--IES Workflow",
"SR: Request to Reset Password - PWM--BIEI Workflow",
"SR: Request to Reset Password - PWM--IES Workflow",
"SR: ZZZ_Internet Access unblock request - PWM (DO NOT USE)--BIEI Workflow",
"SR: ZZZ_Internet Access unblock request (DO NOT USE)--BIEI Workflow",
"SR: ZZZ_Internet Banking Client Support - PWM (DO NOT USE)--BIEI Workflow",
"SR: ZZZ_Internet Banking Client Support (DO NOT USE)--BIEI Workflow",
"SR: ZZZ_Internet Banking Client Support (DO NOT USE)--IES Workflow",
"SR: Contract/Document Review Request--INTERNAL Workflow",
"SR: General Legal Approval--INTERNAL Workflow",
"SR: Request for Opinion--INTERNAL Workflow",
"SR: Review Vendor Contract--INTERNAL Workflow",
"SR: Personal Banking (BIEI + IES) Account Opening--BIEI Workflow",
"SR: Client Answer Slip--BIEI Workflow",
"SR: Suitability Blocking--BIEI Workflow",
"SR: Suitability Blocking--IES Workflow",
"SR: Suitability Deviation--BIEI Workflow",
"SR: Business Cards--INTERNAL Workflow",
"SR: Miami Communications--INTERNAL Workflow",
"SR: Translations and Look and Feel--INTERNAL Workflow",
"SR: Mass Mailing Request--INTERNAL Workflow",
"SR: Corestone Setup--IES Workflow",
"SR: Custody Support--IES Workflow",
"SR: Debit card activation and Notification of client travel--IES Workflow",
"SR: MCC Account Maintenance--IES Workflow",
"SR: MCC Checkbook Order--IES Workflow",
"SR: Cambio Perfil Cliente- Investment Profile--INTERNAL Workflow",
"SR: Egreso Custodia--INTERNAL Workflow",
"SR: Orden RF--BIEI Workflow",
"SR: Test Flow: Account Opening--INTERNAL Workflow",
"SR: Guarantee Services--BIEI Workflow",
"SR: Guarantee Services--IES Workflow",
"SR: Guarantee Services--INTERNAL Workflow",
"SR: Trade Corrections--IES Workflow",
"SR: Trade Corrections--BIEI Workflow",
"SR: Mutual/Hedge Funds Service : New Account in Olympic--INTERNAL Workflow",
"SR: Approved MSD Private Equity - 023--BIEI Workflow",
"SR: Approved Private Equity & Direct Real Estate - 022--BIEI Workflow",
"SR: Approved Single Manager Hedge Funds - 021--BIEI Workflow",
"SR: Open a New Portfolio--BIEI Workflow",
"SR: Cost Price Challenge--BIEI Workflow",
"SR: Cost Price Challenge--IES Workflow",
"SR: Market Price Challenge--BIEI Workflow",
"SR: Market Price Challenge--IES Workflow",
"SR: Documentations Update--BIEI Workflow",
"SR: Block to Close--BIEI Workflow",
"SR: Block to Close--IES Workflow",
"SR: Close Cash ONLY--BIEI Workflow",
"SR: Closing with Assets--BIEI Workflow",
"SR: Closing with default securities and/or hedge funds--BIEI Workflow",
"SR: End of the month review--BIEI Workflow",
"SR: Request of Credit Facility – Itau Brazil--INTERNAL Workflow",
"SR: Report New Returned Mail--BIEBT Workflow",
"SR: Report New Returned Mail--BIEI Workflow",
"SR: Resolved Returned Mail--BIEI Workflow",
"SR: Resolved Returned Mail--IES Workflow",
"SR: JT3's--IES Workflow",
"SR: KYC Renewals--INTERNAL Workflow",
"SR: New Accounts/Daily work/Others--INTERNAL Workflow",
"SR: SN Itau products pricing--INTERNAL Workflow",
"SR: SN Product Pricing - NON ITAU--INTERNAL Workflow",
"SR: Mailing Copy of Official Statement--BIEI Workflow",
"SR: Mailing Copy of official statement (no charge to client)--BIEI Workflow",
"SR: Mailing Copy of Official Statements--IES Workflow",
"SR: Mailing Copy of Official Statements (no charge to client)--IES Workflow",
"SR: Operational Issues--BIEI Workflow",
"SR: Test Carlos--BIEI Workflow",
"SR: Test Carlos--IES Workflow",
"SR: Approved Private Equity for IFO or All In Accounts--BIEI Workflow",
"SR: CD 50K zero coupon Buy (LOA only)--BIEI Workflow",
"SR: CD 50K zero coupon Buy (LOA only)--IES Workflow",
"SR: CD 50K zero coupon Buy (Phone only)--BIEI Workflow",
"SR: CD 50K zero coupon Buy (Phone only)--IES Workflow",
"SR: CDs - sell only--BIEBT Workflow",
"SR: CDs Zero Coupon Buy (LOA Only)--BIEI Workflow",
"SR: CDs Zero Coupon Buy (LOA Only)--IES Workflow",
"SR: CDs Zero Coupon Buy (Phone Only)--BIEI Workflow",
"SR: CDs Zero Coupon Buy (Phone Only)--IES Workflow",
"SR: CDs Zero Coupon Sell (LOA Only)--BIEI Workflow",
"SR: CDs Zero Coupon Sell (LOA Only)--IES Workflow",
"SR: CDs Zero Coupon Sell (Phone Only)--BIEI Workflow",
"SR: CDs Zero Coupon Sell (Phone Only)--IES Workflow",
"SR: De Minimis Exemptions--BIEI Workflow",
"SR: Eligible Contract Participant Approval--BIEI Workflow",
"SR: Equities, ETFs and Options--BIEI Workflow",
"SR: Equities, ETFs and Options--IES Workflow",
"SR: Equities, ETFs and Options - sell only--BIEBT Workflow",
"SR: Fixed Income--BIEI Workflow",
"SR: Fixed Income--IES Workflow",
"SR: Fixed Income - sell only--BIEBT Workflow",
"SR: FX--BIEI Workflow",
"SR: FX--IES Workflow",
"SR: FX - sell only--BIEBT Workflow",
"SR: Mutual & Hedge Funds--BIEI Workflow",
"SR: Mutual & Hedge Funds--IES Workflow",
"SR: Non approved Mutual/ Hedge Funds--BIEI Workflow",
"SR: Non approved Mutual/ Hedge Funds--IES Workflow",
"SR: Non Approved Private Equities Funds--BIEI Workflow",
"SR: Non Approved Private Equities Funds--IES Workflow",
"SR: Structured Products - sell only--BIEBT Workflow",
"SR: Structured Products for Regional Offices--BIEI Workflow",
"SR: Structured Products for Regional Offices--IES Workflow",
"SR: Structured Products SELL (Registered Reps Only)--BIEI Workflow",
"SR: Structured Products SELL (Registered Reps Only)--IES Workflow",
"SR: Switch of Mutual Funds--BIEI Workflow",
"SR: TC below USD 25,000 (ONLY LOA)--BIEI Workflow",
"SR: TC below USD 25,000 (ONLY PHONE)--BIEI Workflow",
"SR: Trading - Multiple order - Brazil Regional Offices ONLY--BIEI Workflow",
"SR: All other products--BIEI Workflow",
"SR: Structured Products BUY (Registered Reps Only)--BIEI Workflow",
"SR: Structured Products Contingent Orders--BIEI Workflow",
"SR: Trading for credit clients--IES Workflow",
"SR: Mutual and Hedge Funds ONLY--BIEI Workflow",
"SR: Hedge.Mutual Funds--BIEI Workflow",
"SR: FX trades for Chilean omnibus account--BIEI Workflow",
"SR: Security trades for Chilean omnibus account--BIEI Workflow",
"SR: TD Cancellation or Amendment--BIEBT Workflow",
"SR: TD Cancellation or Amendment--BIEI Workflow",
"SR: TD New--BIEI Workflow",
"SR: TD New for Credit Clients--BIEI Workflow",
"SR: Inquiries and Investigations--BIEI Workflow",
"SR: Inquiries and Investigations--IES Workflow",
"SR: Bahamas Migration--BIEBT Workflow",
"SR: Reason 01: Attempted Fraud / Fraud / OFAC--BIEI Workflow",
"SR: Reason 01: Attempted Fraud / Fraud / OFAC--IES Workflow",
"SR: Reason 01: Attempted Fraud or Fraud, Deceased, OFAC, CAAML Close--BIEBT Workflow",
"SR: Reason 02: Block to close / Closing <200K / CLOSING PJCT HR<1MM--BIEI Workflow",
"SR: Reason 02: Block to close / Closing <200K / CLOSING PJCT HR<1MM--IES Workflow",
"SR: Reason 02: Closing Project Account--BIEBT Workflow",
"SR: Reason 03: Block to close / Closing <200K / CLOSING PJCT HR<1MM--IES Workflow",
"SR: Reason 03: Credit--BIEBT Workflow",
"SR: Reason 03: Credit / CREDIT-CROSS PLEDGER / OD OVER 30 DAYS--BIEI Workflow",
"SR: Reason 04: Domestic--BIEBT Workflow",
"SR: Reason 04: Domestic--BIEI Workflow",
"SR: Reason 04: Domestic--IES Workflow",
"SR: Reason 05: Compliance, Stolen Checks, Lost Statements--BIEBT Workflow",
"SR: Reason 05: Compliance, Stolen Checks, Lost Statements--BIEI Workflow",
"SR: Reason 05: Compliance, Stolen Checks, Lost Statements--IES Workflow",
"SR: Reason 07: Hold Mail--BIEI Workflow",
"SR: Reason 08: AML MONITOR IN. DEP / AML TO CLOSE / B. SHARE CERT EXP / F-UP ARTICLES - BVI / KYC EXP OVER 1 YR / NEG MEDIA--BIEI Workflow",
"SR: Reason 08: KYC, Bearer Share Certificate Expired or CAAML or Neg Media--BIEBT Workflow",
"SR: Reason 08: MONITOR IN. DEP / AML TO CLOSE / B. SHARE CERT EXP / F-UP ARTICLES - BVI / KYC EXP OVER 1 YR / NEG MEDIA--IES Workflow",
"SR: Reason 08: Trading--BIEI Workflow",
"SR: Reason 09: CHANGE OF OWNERSHIP / DORMANT / MIGRATE TO MIA / MISSING DOCS / NOT SUBJ TO MIGRAT. / ONLINE BK ENROLLMENT / RETURNED MAIL--BIEI Workflow",
"SR: Reason 09: CHANGE OF OWNERSHIP / DORMANT / MIGRATE TO MIA / MISSING DOCS / NOT SUBJ TO MIGRAT. / ONLINE BK ENROLLMENT / RETURNED MAIL--IES Workflow",
"SR: Reason 09: Missing Documentation, Returned Mail, Dormant, Online Banking Enrollment--BIEBT Workflow",
"SR: Reason 10: Escrow--BIEI Workflow",
"SR: Reason 11: Trading--BIEI Workflow",
"SR: Reason 11: Transaction Surveillance Issues--BIEBT Workflow",
"SR: Reason 11: Transaction Surveillance Issues--IES Workflow",
"SR: Reason 11: Transaction Surveillance Issues (TSU)--BIEI Workflow",
"SR: Reason 12: Investigation--BIEI Workflow",
"SR: Reason 12: Investigation--IES Workflow",
"SR: Reason 13: Bearer Shares Closing--BIEBT Workflow",
"SR: Reason 13: Bearer Shares Closing--BIEI Workflow",
"SR: Reason 14: Defaulted Securities--BIEBT Workflow",
"SR: Reason 14: Defaulted Securities--BIEI Workflow",
"SR: Reason 15: Fiduciary Doc Issue--BIEI Workflow",
"SR: Reason 17: Family Dispute--BIEI Workflow",
"SR: Reason 18: Legal--BIEI Workflow",
"SR: Reason 18: Legal and Deceased--IES Workflow",
"SR: Reason 19: Suitability--BIEI Workflow",
"SR: Reason 19:Suitability--IES Workflow",
"SR: Reason: Hold Mail--BIEBT Workflow",
"SR: Client related trip 2--INTERNAL Workflow",
"SR: Non Client related trip--INTERNAL Workflow",
"SR: Miami Brazil--BIEI Workflow",
"SR: Miami Hispanic--BIEI Workflow",
"SR: Regional Brazil--BIEI Workflow",
"SR: Regional Paraguay--BIEI Workflow",
"SR: Place Account on Watch--BIEI Workflow",
"SR: Place Account on Watch--IES Workflow",
"SR: Remove Account from Watch--BIEI Workflow",
"SR: Remove Account from Watch--IES Workflow",
"SR: Renewal of loans with standing instructions which is d--BIEI Workflow",
"SR: Itau Forms - IIS Workflow",
"SR: MCC Issues Workflow",
"SR: Renewal of loans with standing instructions which is done via e-mail Workflow",
"SR: Request Changes on the Service Request Workflow",
"SR: Signature Verification for Credit Documents Workflow",
"SR: VISA approvals for wires, billpayer, Amex, Custody, fees, etc. Workflow",
"Account Closing",
"AMA: Process Management Workflow",
"Expired CIP",
"Copy of SR: Outgoing Assets - Bonds and/or Equities Workflow",
"Copy of SR: Incoming Assets - Hedge Funds Workflow",
"AO: Task Management Workflow",
"AO: 4MC Account Opening Migration--BIEI Workflow",
"AO: 4MC Account Opening Migration with non approved Mutual--BIEI Workflow",
"AO: Account Opening Kit Preparation--IES Workflow",
"AO: Account Replication--IES Workflow",
"AO: Account Replication--BIEI Workflow",
"AO: Account Replication / Re-Opening--IES Workflow",
"AO: Corporation Incorporation--IES Workflow",
"AO: Corporation Incorporation--BIEI Workflow",
"AO: Corporation Incorporation - High Risk--BIEI Workflow",
"AO: Corporation Incorporation MIA High Risk--IES Workflow",
"AO: Corporation Incorporation MIA High Risk--BIEI Workflow",
"AO: Corporation Incorporation MIA Low-Medium Risk--BIEI Workflow",
"AO: Corporation Incorporation MIA Low-Medium Risk--IES Workflow",
"AO: Corporation Incorporation Switzerland High Risk--IES Workflow",
"AO: Corporation Incorporation Switzerland High Risk--BIEI Workflow",
"AO: Corporation Incorporation Switzerland Low-Medium Risk--BIEI Workflow",
"AO: Corporation Incorporation Switzerland Low-Medium Risk--IES Workflow",
"AO: E-Signature - Personal Banking--IES Workflow",
"AO: E-Signature - RMs based in Miami - Private Banking (BI--BIEI Workflow",
"AO: FX Account Opening--BIEI Workflow",
"AO: JT1 Replication - Migration Project Uruguay 2014--BIEI Workflow",
"AO: JT3 migration--BIEI Workflow",
"AO: JT3 Migration 2014--BIEI Workflow",
"AO: JT3 Migration 2014 - Defaulted Security--BIEI Workflow",
"AO: Money Market Account--BIEI Workflow",
"AO: Money Market Account Opening--IES Workflow",
"AO: Only for Doc. Team use--BIEI Workflow",
"AO: Only for Doc. Team use--IES Workflow",
"AO: Personal Banking Conversion--BIEI Workflow",
"AO: Personal Banking Conversion - Defaulted Security--BIEI Workflow",
"AO: Regional Office RM - Bank ONLY--BIEI Workflow",
"AO: Regional Office RM - Bank ONLY - High Risk--BIEI Workflow",
"AO: Regional Offices - Private Banking (BIEI+IES)--BIEI Workflow",
"AO: Regional Offices - Private Banking (BIEI+IES) NEW!--BIEI Workflow",
"AO: Regional Offices - Private Banking (BIEI+IES) NEW! - H--BIEI Workflow",
"AO: Regional Offices JT5/JT3--IES Workflow",
"AO: Regional Offices JT5/JT3 - High Risk--IES Workflow",
"AO: Re-Opening Account--IES Workflow",
"AO: Re-Opening Account--BIEI Workflow",
"AO: Re-Opening Account - High Risk--BIEI Workflow",
"AO: RM Based in Miami - Bank ONLY--BIEI Workflow",
"AO: RM Based in Miami - Bank ONLY- High Risk--BIEI Workflow",
"AO: RMs based in Miami - Personal Banking (JT3)--IES Workflow",
"AO: RMs based in Miami - Personal Banking (JT3) NEW!--IES Workflow",
"AO: RMs based in Miami - Private Banking (BIEI+IES)--BIEI Workflow",
"AO: RMs based in Miami - Private Banking (BIEI+IES) NEW!--BIEI Workflow",
"AO: RMs based in Miami - Private Banking (BIEI+IES) NEW! ---BIEI Workflow",
"AO: Special Money Market Account--BIEI Workflow",
"AO: Upgrade RORA to Managed Company--BIEI Workflow",
"AO: Uruguay Conversion 2014--BIEI Workflow",
"AO: Uruguay Migration 2014--BIEI Workflow",
"MCC2: Process Management Workflow",
"Actualiza Antecedentes Contacto y Bancarios",
"Actualiza Perfil Inversionista Clientes",
"DP: Process Management Workflow",
"DCBE Update Workflow",
"Apertura Cuenta",
"Actualiza Antecedentes Legales de Clientes",
"TUR: Process Management Workflow",
"TEST: Process Management Workflow",
"Copy of AM: Customer Information update--BIEI Workflow",
"Copy of AM: Change of Address or Phones: Callback Required--BIEI Workflow",
"Copy of AM: Change of Address or Phones: No Callback Required--BIEI Workflow",
"Copy of AM: W8 Update--BIEI Workflow",
"Copy of UA: Reason 03: Credit / CREDIT-CROSS PLEDGER / OD OVER 30 --BIEI Workflow",
"Copy of Expired CIP",
 };
      foreach (var wf in workflows)
        (await Rest.DeleteWorkflowAsync(wf).WithError()).SideEffect(t => Debug.WriteLine(t));
    }

    private static async Task TestAlreadyExists(Task t) {
      await ExceptionAssert.Propagates<HttpResponseMessageException>(t,
        exc => Assert.AreEqual(ResponceErrorType.AlreadyExists, exc.ResponseErrorType));
    }

    [TestMethod]
    public async Task GetWorkflowShemeWorkflowAsync() {
      var ret = await RestMonad.Empty().GetWorkflowShemeWorkflowAsync(RestConfiger.WorkflowSchemaIds[0]);
      Console.WriteLine(ret.ToJson());
      Assert.AreEqual(4, ret.Value.Count());
    }
  }
}
