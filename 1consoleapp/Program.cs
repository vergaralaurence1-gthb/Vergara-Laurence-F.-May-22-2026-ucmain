using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace LibraryLoansSystem
{
    // Member Model
    public class LibraryMember
    {
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public DateTime RegisteredDate { get; set; }
        public DateTime LastLoanDate { get; set; }

        public override string ToString()
        {
            return $"{MemberId}|{MemberName}|{RegisteredDate:yyyy-MM-dd HH:mm:ss}|{LastLoanDate:yyyy-MM-dd HH:mm:ss}";
        }

        public static LibraryMember FromString(string line)
        {
            var parts = line.Split('|');
            return new LibraryMember
            {
                MemberId = parts[0],
                MemberName = parts[1],
                RegisteredDate = DateTime.Parse(parts[2]),
                LastLoanDate = DateTime.Parse(parts[3])
            };
        }
    }

    // Record Model for Library Loans
    public class LibraryLoan
    {
        public string RecordId { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public DateTime LoanDate { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsReturned { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public string Checksum { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{RecordId}|{MemberId}|{MemberName}|{BookTitle}|{ISBN}|{LoanDate:yyyy-MM-dd}|{DueDate:yyyy-MM-dd}|{IsReturned}|{CreatedAt:yyyy-MM-dd HH:mm:ss}|{UpdatedAt:yyyy-MM-dd HH:mm:ss}|{IsActive}|{Checksum}";
        }

        public static LibraryLoan FromString(string line)
        {
            var parts = line.Split('|');
            return new LibraryLoan
            {
                RecordId = parts[0],
                MemberId = parts[1],
                MemberName = parts[2],
                BookTitle = parts[3],
                ISBN = parts[4],
                LoanDate = DateTime.Parse(parts[5]),
                DueDate = DateTime.Parse(parts[6]),
                IsReturned = bool.Parse(parts[7]),
                CreatedAt = DateTime.Parse(parts[8]),
                UpdatedAt = DateTime.Parse(parts[9]),
                IsActive = bool.Parse(parts[10]),
                Checksum = parts[11]
            };
        }
    }

    // Validation Component
    public static class ValidationHelper
    {
        public static bool ValidateMemberName(string? name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Length >= 2 && name.Length <= 50;
        }

        public static bool ValidateBookTitle(string? title)
        {
            return !string.IsNullOrWhiteSpace(title) && title.Length >= 2 && title.Length <= 100;
        }

        public static bool ValidateISBN(string? isbn)
        {
            return !string.IsNullOrWhiteSpace(isbn) && (isbn.Length == 10 || isbn.Length == 13);
        }

        public static bool ValidateDates(DateTime loanDate, DateTime dueDate)
        {
            return loanDate <= dueDate && loanDate <= DateTime.Now;
        }

        public static string ComputeLoanChecksum(LibraryLoan loan)
        {
            string data = $"{loan.MemberId}|{loan.MemberName}|{loan.BookTitle}|{loan.ISBN}|{loan.LoanDate}|{loan.DueDate}|{loan.IsReturned}|{loan.CreatedAt}|{loan.UpdatedAt}|{loan.IsActive}";
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(data);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        public static bool VerifyLoanChecksum(LibraryLoan loan)
        {
            string computedChecksum = ComputeLoanChecksum(loan);
            return computedChecksum == loan.Checksum;
        }
    }

    // Audit Logger
    public static class AuditLogger
    {
        private static readonly string auditFilePath = "Data/audit.log";

        static AuditLogger()
        {
            Directory.CreateDirectory("Data");
        }

        public static void Log(string action, string details)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {action} | {details}";
                File.AppendAllText(auditFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to audit log: {ex.Message}");
            }
        }

        public static void LogError(string operation, Exception ex)
        {
            Log("ERROR", $"{operation} - {ex.Message}");
        }
    }

    // Member Repository
    public class MemberRepository
    {
        private readonly string memberFile = "Data/members.txt";
        private List<LibraryMember> members = new List<LibraryMember>();

        public MemberRepository()
        {
            InitializeStorage();
            LoadMembers();
        }

        private void InitializeStorage()
        {
            Directory.CreateDirectory("Data");
            if (!File.Exists(memberFile))
            {
                File.Create(memberFile).Close();
            }
        }

        private void LoadMembers()
        {
            members = new List<LibraryMember>();
            try
            {
                if (File.Exists(memberFile))
                {
                    var lines = File.ReadAllLines(memberFile);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            try
                            {
                                var member = LibraryMember.FromString(line);
                                members.Add(member);
                            }
                            catch (Exception ex)
                            {
                                AuditLogger.LogError("LoadMembers", ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("LoadMembers", ex);
            }
        }

        private void SaveMembers()
        {
            try
            {
                var lines = members.Select(m => m.ToString()).ToArray();
                File.WriteAllLines(memberFile, lines);
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("SaveMembers", ex);
                throw;
            }
        }

        public LibraryMember? FindMemberByName(string name)
        {
            return members.FirstOrDefault(m => m.MemberName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public List<LibraryMember> FindAllMembersByName(string name)
        {
            return members.Where(m => m.MemberName.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public LibraryMember? GetMemberById(string id)
        {
            return members.FirstOrDefault(m => m.MemberId == id);
        }

        public string GetOrCreateMember(string memberName)
        {
            var existingMember = FindMemberByName(memberName);
            
            if (existingMember != null)
            {
                return existingMember.MemberId;
            }
            
            string newMemberId = GenerateMemberId();
            var newMember = new LibraryMember
            {
                MemberId = newMemberId,
                MemberName = memberName,
                RegisteredDate = DateTime.Now,
                LastLoanDate = DateTime.Now
            };
            
            members.Add(newMember);
            SaveMembers();
            AuditLogger.Log("MEMBER_CREATED", $"Created new member ID: {newMemberId}, Name: {memberName}");
            
            return newMemberId;
        }

        public void UpdateLastLoanDate(string memberId)
        {
            var member = GetMemberById(memberId);
            if (member != null)
            {
                member.LastLoanDate = DateTime.Now;
                SaveMembers();
            }
        }

        private string GenerateMemberId()
        {
            int nextNumber = members.Count + 1;
            return $"MEM-{nextNumber:D3}";
        }
    }

    // File Repository for Loans
    public class LoanRepository
    {
        private readonly string dataFile = "Data/library_loans.txt";
        private List<LibraryLoan> loans = new List<LibraryLoan>();
        private MemberRepository memberRepo;

        public LoanRepository(MemberRepository memberRepo)
        {
            this.memberRepo = memberRepo;
            InitializeStorage();
            LoadData();
        }

        private void InitializeStorage()
        {
            Directory.CreateDirectory("Data");
            if (!File.Exists(dataFile))
            {
                File.Create(dataFile).Close();
            }
        }

        private void LoadData()
        {
            loans = new List<LibraryLoan>();
            try
            {
                if (File.Exists(dataFile))
                {
                    var lines = File.ReadAllLines(dataFile);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            try
                            {
                                var loan = LibraryLoan.FromString(line);
                                if (ValidationHelper.VerifyLoanChecksum(loan))
                                {
                                    loans.Add(loan);
                                }
                                else
                                {
                                    AuditLogger.Log("WARNING", $"Checksum failed for loan ID: {loan.RecordId}");
                                }
                            }
                            catch (Exception ex)
                            {
                                AuditLogger.LogError("LoadData", ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("LoadData", ex);
                Console.WriteLine("Error loading data. Starting with empty database.");
            }
        }

        private void SaveData()
        {
            try
            {
                var lines = loans.Select(l => l.ToString()).ToArray();
                File.WriteAllLines(dataFile, lines);
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("SaveData", ex);
                throw;
            }
        }

        public void AddLoan(LibraryLoan loan)
        {
            loan.RecordId = GenerateUniqueId();
            loan.CreatedAt = DateTime.Now;
            loan.UpdatedAt = DateTime.Now;
            loan.IsActive = true;
            loan.IsReturned = false;
            loan.Checksum = ValidationHelper.ComputeLoanChecksum(loan);
            
            loans.Add(loan);
            SaveData();
            memberRepo.UpdateLastLoanDate(loan.MemberId);
            AuditLogger.Log("ADD", $"Added loan ID: {loan.RecordId}, Member: {loan.MemberName}, Book: {loan.BookTitle}");
        }

        public List<LibraryLoan> GetAllActiveLoans()
        {
            return loans.Where(l => l.IsActive && !l.IsReturned).ToList();
        }

        public List<LibraryLoan> GetAllLoans()
        {
            return loans.ToList();
        }

        public LibraryLoan? GetLoanById(string id)
        {
            return loans.FirstOrDefault(l => l.RecordId == id);
        }

        public void UpdateLoan(LibraryLoan loan)
        {
            var index = loans.FindIndex(l => l.RecordId == loan.RecordId);
            if (index != -1)
            {
                loan.UpdatedAt = DateTime.Now;
                loan.Checksum = ValidationHelper.ComputeLoanChecksum(loan);
                loans[index] = loan;
                SaveData();
                AuditLogger.Log("UPDATE", $"Updated loan ID: {loan.RecordId}");
            }
        }

        public void ReturnBook(string id)
        {
            var loan = GetLoanById(id);
            if (loan != null && !loan.IsReturned)
            {
                loan.IsReturned = true;
                loan.IsActive = false;
                loan.UpdatedAt = DateTime.Now;
                loan.Checksum = ValidationHelper.ComputeLoanChecksum(loan);
                SaveData();
                AuditLogger.Log("RETURN", $"Book returned for loan ID: {id}, Member: {loan.MemberName}");
            }
        }

        public void SoftDelete(string id)
        {
            var loan = GetLoanById(id);
            if (loan != null && loan.IsActive)
            {
                loan.IsActive = false;
                loan.UpdatedAt = DateTime.Now;
                loan.Checksum = ValidationHelper.ComputeLoanChecksum(loan);
                SaveData();
                AuditLogger.Log("SOFT_DELETE", $"Soft deleted loan ID: {id}");
            }
        }

        public void HardDelete(string id)
        {
            var loan = GetLoanById(id);
            if (loan != null)
            {
                loans.Remove(loan);
                SaveData();
                AuditLogger.Log("HARD_DELETE", $"Hard deleted loan ID: {id}, Member: {loan.MemberName}");
            }
        }

        private string GenerateUniqueId()
        {
            return $"LOAN_{DateTime.Now.Ticks}_{new Random().Next(1000, 9999)}";
        }

        public List<LibraryLoan> SearchLoans(string keyword)
        {
            return loans.Where(l => l.IsActive && 
                (l.MemberName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                 l.BookTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                 l.MemberId.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                 l.ISBN.Contains(keyword)))
                .ToList();
        }

        public List<LibraryLoan> GetOverdueLoans()
        {
            return loans.Where(l => l.IsActive && !l.IsReturned && l.DueDate < DateTime.Now)
                        .OrderBy(l => l.DueDate)
                        .ToList();
        }

        public List<LibraryLoan> GetLoansByMember(string memberId)
        {
            return loans.Where(l => l.MemberId == memberId).OrderByDescending(l => l.LoanDate).ToList();
        }

        public int GetActiveLoanCountForMember(string memberId)
        {
            return loans.Count(l => l.MemberId == memberId && l.IsActive && !l.IsReturned);
        }
    }

    // Report Generator
    public class ReportGenerator
    {
        private readonly LoanRepository loanRepo;
        private readonly MemberRepository memberRepo;

        public ReportGenerator(LoanRepository loanRepo, MemberRepository memberRepo)
        {
            this.loanRepo = loanRepo;
            this.memberRepo = memberRepo;
        }

        public void GenerateOverdueReport()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("        OVERDUE BOOKS REPORT");
            Console.WriteLine("========================================\n");
            
            var overdue = loanRepo.GetOverdueLoans();
            
            if (overdue.Count == 0)
            {
                Console.WriteLine("No overdue books! All loans are on time.\n");
            }
            else
            {
                Console.WriteLine($"{"Member ID",-12} {"Member Name",-22} {"Book Title",-30} {"Due Date",-12} {"Days Overdue",-12}");
                Console.WriteLine(new string('-', 88));
                
                foreach (var loan in overdue)
                {
                    int daysOverdue = (DateTime.Now - loan.DueDate).Days;
                    Console.WriteLine($"{loan.MemberId,-12} {loan.MemberName,-22} {loan.BookTitle,-30} {loan.DueDate:yyyy-MM-dd,-12} {daysOverdue,-12}");
                }
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        public void GenerateMemberLoansReport()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("        MEMBER LOANS REPORT");
            Console.WriteLine("========================================\n");
            
            Console.Write("Enter Member Name or Member ID: ");
            string? search = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(search))
            {
                Console.WriteLine("No search term entered!");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }
            
            var members = memberRepo.FindAllMembersByName(search);
            var memberById = memberRepo.GetMemberById(search);
            if (memberById != null && !members.Contains(memberById))
            {
                members.Add(memberById);
            }
            
            if (members.Count == 0)
            {
                Console.WriteLine($"No members found matching: {search}");
            }
            else
            {
                foreach (var member in members)
                {
                    var memberLoans = loanRepo.GetLoansByMember(member.MemberId);
                    int activeCount = loanRepo.GetActiveLoanCountForMember(member.MemberId);
                    
                    Console.WriteLine($"\nMEMBER: {member.MemberName} (ID: {member.MemberId})");
                    Console.WriteLine($"Registered: {member.RegisteredDate:MMMM dd, yyyy}");
                    Console.WriteLine($"Active Loans: {activeCount}");
                    Console.WriteLine($"Total Loans: {memberLoans.Count}");
                    Console.WriteLine(new string('-', 50));
                    
                    if (memberLoans.Count == 0)
                    {
                        Console.WriteLine("No loan history found.");
                    }
                    else
                    {
                        Console.WriteLine($"{"Book Title",-35} {"Loan Date",-12} {"Due Date",-12} {"Status",-10}");
                        Console.WriteLine(new string('-', 71));
                        
                        foreach (var loan in memberLoans)
                        {
                            string status = loan.IsReturned ? "RETURNED" : 
                                           (loan.DueDate < DateTime.Now ? "OVERDUE" : "ACTIVE");
                            Console.WriteLine($"{loan.BookTitle,-35} {loan.LoanDate:yyyy-MM-dd,-12} {loan.DueDate:yyyy-MM-dd,-12} {status,-10}");
                        }
                    }
                    Console.WriteLine();
                }
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        public void GenerateSummaryReport()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("        LIBRARY SUMMARY REPORT");
            Console.WriteLine("========================================\n");
            
            var allLoans = loanRepo.GetAllLoans();
            var activeLoans = loanRepo.GetAllActiveLoans();
            var overdueLoans = loanRepo.GetOverdueLoans();
            var returnedLoans = allLoans.Where(l => l.IsReturned).ToList();
            var allMembers = memberRepo.FindAllMembersByName("");
            
            Console.WriteLine($"TOTAL MEMBERS:      {allMembers.Count}");
            Console.WriteLine($"TOTAL LOANS:        {allLoans.Count}");
            Console.WriteLine($"ACTIVE LOANS:       {activeLoans.Count}");
            Console.WriteLine($"RETURNED LOANS:     {returnedLoans.Count}");
            Console.WriteLine($"OVERDUE LOANS:      {overdueLoans.Count}");
            
            if (activeLoans.Count > 0)
            {
                Console.WriteLine("\nCURRENTLY BORROWED BOOKS:\n");
                foreach (var loan in activeLoans.Take(5))
                {
                    string dueStatus = loan.DueDate < DateTime.Now ? "OVERDUE" : $"Due: {loan.DueDate:yyyy-MM-dd}";
                    Console.WriteLine($"  - {loan.BookTitle} | {loan.MemberName} (ID: {loan.MemberId}) | {dueStatus}");
                }
                if (activeLoans.Count > 5)
                    Console.WriteLine($"  ... and {activeLoans.Count - 5} more");
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    // Main Program
    class Program
    {
        private static MemberRepository? memberRepo;
        private static LoanRepository? loanRepo;
        private static ReportGenerator? reportGenerator;

        static void Main(string[] args)
        {
            Console.Title = "Library Loans Management System";
            
            memberRepo = new MemberRepository();
            loanRepo = new LoanRepository(memberRepo);
            reportGenerator = new ReportGenerator(loanRepo, memberRepo);
            
            AuditLogger.Log("APP_START", "Library Loans System started");
            
            bool exit = false;
            while (!exit)
            {
                DisplayMainMenu();
                string? choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        AddLoan();
                        break;
                    case "2":
                        ViewLoans();
                        break;
                    case "3":
                        ReturnBook();
                        break;
                    case "4":
                        UpdateLoan();
                        break;
                    case "5":
                        DeleteLoan();
                        break;
                    case "6":
                        HardDeleteLoan();
                        break;
                    case "7":
                        GenerateReports();
                        break;
                    case "8":
                        ViewAuditLog();
                        break;
                    case "9":
                        exit = true;
                        AuditLogger.Log("APP_EXIT", "Application closed");
                        Console.WriteLine("\nThank you for using Library Loans System! Goodbye!\n");
                        break;
                    default:
                        Console.WriteLine("\nInvalid option! Please choose 1-9.");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void DisplayMainMenu()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("    LIBRARY LOANS MANAGEMENT SYSTEM");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("[1] Borrow a Book (New Loan)");
            Console.WriteLine("[2] View Loans (Search/Filter)");
            Console.WriteLine("[3] Return a Book");
            Console.WriteLine("[4] Extend Due Date");
            Console.WriteLine("[5] Soft Delete (Archive Loan)");
            Console.WriteLine("[6] Hard Delete (Permanent Remove)");
            Console.WriteLine("[7] Generate Reports");
            Console.WriteLine("[8] View Audit Log");
            Console.WriteLine("[9] Exit");
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.Write("Enter your choice: ");
        }

        static void AddLoan()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("           BORROW A BOOK");
            Console.WriteLine("========================================\n");
            
            try
            {
                string? memberName;
                string memberId;
                
                Console.WriteLine("Please enter the following information:\n");
                
                // Member Name
                do
                {
                    Console.Write("Member Name (2-50 chars): ");
                    memberName = Console.ReadLine();
                    if (!ValidationHelper.ValidateMemberName(memberName))
                    {
                        Console.WriteLine("Invalid! Name must be 2-50 characters.");
                    }
                    else
                    {
                        break;
                    }
                } while (true);
                
                // Check for existing member with same name
                var existingMembers = memberRepo?.FindAllMembersByName(memberName!) ?? new List<LibraryMember>();
                
                if (existingMembers.Count > 0)
                {
                    Console.WriteLine("\n--------------------------------------------------");
                    Console.WriteLine($"A member with name '{memberName}' already exists!");
                    
                    foreach (var member in existingMembers)
                    {
                        Console.WriteLine($"\nExisting Member Details:");
                        Console.WriteLine($"  Member ID: {member.MemberId}");
                        Console.WriteLine($"  Member Name: {member.MemberName}");
                        Console.WriteLine($"  Registered: {member.RegisteredDate:MMMM dd, yyyy}");
                        Console.WriteLine($"  Active Loans: {loanRepo?.GetActiveLoanCountForMember(member.MemberId) ?? 0}");
                    }
                    
                    Console.Write("\nAre you the SAME person as any of the above? (y/n): ");
                    string? isSame = Console.ReadLine();
                    
                    if (isSame?.ToLower() == "y")
                    {
                        Console.Write("\nEnter the Member ID you want to use: ");
                        string? selectedId = Console.ReadLine();
                        var selectedMember = memberRepo?.GetMemberById(selectedId ?? "");
                        
                        if (selectedMember != null)
                        {
                            memberId = selectedMember.MemberId;
                            memberName = selectedMember.MemberName;
                            Console.WriteLine($"\nUsing existing Member ID: {memberId}");
                        }
                        else
                        {
                            Console.WriteLine("\nInvalid Member ID. Creating new member...");
                            memberId = memberRepo?.GetOrCreateMember(memberName!) ?? "";
                        }
                    }
                    else
                    {
                        Console.WriteLine("\nCreating NEW member profile...");
                        memberId = memberRepo?.GetOrCreateMember(memberName!) ?? "";
                        Console.WriteLine($"New Member ID: {memberId}");
                    }
                }
                else
                {
                    Console.WriteLine("\nNew member detected. Creating profile...");
                    memberId = memberRepo?.GetOrCreateMember(memberName!) ?? "";
                    Console.WriteLine($"Member ID: {memberId}");
                }
                
                var loan = new LibraryLoan();
                loan.MemberId = memberId;
                loan.MemberName = memberName!;
                
                // Book Title
                do
                {
                    Console.Write("\nBook Title (2-100 chars): ");
                    string? input = Console.ReadLine();
                    if (!ValidationHelper.ValidateBookTitle(input))
                    {
                        Console.WriteLine("Invalid! Title must be 2-100 characters.");
                    }
                    else
                    {
                        loan.BookTitle = input!;
                        break;
                    }
                } while (true);
                
                // ISBN
                do
                {
                    Console.Write("ISBN (10 or 13 digits): ");
                    string? input = Console.ReadLine();
                    if (!ValidationHelper.ValidateISBN(input))
                    {
                        Console.WriteLine("Invalid! ISBN must be 10 or 13 digits.");
                    }
                    else
                    {
                        loan.ISBN = input!;
                        break;
                    }
                } while (true);
                
                // Loan Date
                do
                {
                    Console.Write("Loan Date (yyyy-mm-dd): ");
                    string? input = Console.ReadLine();
                    if (!DateTime.TryParse(input, out DateTime loanDate))
                    {
                        Console.WriteLine("Invalid date format! Use yyyy-mm-dd");
                        continue;
                    }
                    loan.LoanDate = loanDate;
                    break;
                } while (true);
                
                // Due Date
                DateTime suggestedDue = loan.LoanDate.AddDays(14);
                do
                {
                    Console.Write($"Due Date (yyyy-mm-dd) [Suggested: {suggestedDue:yyyy-MM-dd}]: ");
                    string? input = Console.ReadLine();
                    
                    DateTime dueDate;
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        dueDate = suggestedDue;
                    }
                    else if (!DateTime.TryParse(input, out dueDate))
                    {
                        Console.WriteLine("Invalid date format! Use yyyy-mm-dd");
                        continue;
                    }
                    
                    if (!ValidationHelper.ValidateDates(loan.LoanDate, dueDate))
                    {
                        Console.WriteLine("Due date must be after loan date and loan date cannot be in future!");
                    }
                    else
                    {
                        loan.DueDate = dueDate;
                        break;
                    }
                } while (true);
                
                loanRepo?.AddLoan(loan);
                
                Console.WriteLine("\n========================================");
                Console.WriteLine("BOOK BORROWED SUCCESSFULLY!");
                Console.WriteLine($"Loan ID: {loan.RecordId}");
                Console.WriteLine($"Member ID: {loan.MemberId}");
                Console.WriteLine($"Member: {loan.MemberName}");
                Console.WriteLine($"Book: {loan.BookTitle}");
                Console.WriteLine($"Return By: {loan.DueDate:MMMM dd, yyyy}");
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                AuditLogger.LogError("AddLoan", ex);
                Console.WriteLine($"\nError adding loan: {ex.Message}");
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void ViewLoans()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("             VIEW LOANS");
            Console.WriteLine("========================================\n");
            
            Console.WriteLine("Search Options:");
            Console.WriteLine("  - Press ENTER to view all active loans");
            Console.WriteLine("  - Type Member ID (e.g., MEM-001)");
            Console.WriteLine("  - Type Member Name (e.g., Juan)");
            Console.WriteLine("  - Type Book Title (e.g., Programming)");
            Console.Write("\nSearch: ");
            
            string? searchTerm = Console.ReadLine();
            
            List<LibraryLoan> loans;
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                loans = loanRepo?.GetAllActiveLoans() ?? new List<LibraryLoan>();
                Console.WriteLine("\nALL ACTIVE LOANS\n");
            }
            else
            {
                loans = loanRepo?.SearchLoans(searchTerm) ?? new List<LibraryLoan>();
                Console.WriteLine($"\nSEARCH RESULTS FOR '{searchTerm}'\n");
            }
            
            if (loans.Count == 0)
            {
                Console.WriteLine("No loans found.");
            }
            else
            {
                Console.WriteLine($"{"Loan ID",-22} {"Member ID",-10} {"Member Name",-18} {"Book Title",-25} {"Due Date",-12} {"Status",-10}");
                Console.WriteLine(new string('-', 97));
                
                foreach (var loan in loans)
                {
                    string status = loan.DueDate < DateTime.Now ? "OVERDUE" : "ACTIVE";
                    Console.WriteLine($"{loan.RecordId,-22} {loan.MemberId,-10} {loan.MemberName,-18} {loan.BookTitle,-25} {loan.DueDate:yyyy-MM-dd,-12} {status,-10}");
                }
            }
            
            AuditLogger.Log("VIEW", $"Viewed {loans.Count} loans");
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void ReturnBook()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("           RETURN A BOOK");
            Console.WriteLine("========================================\n");
    
            // === ADD THIS PART: Show active loans first ===
            var activeLoans = loanRepo?.GetAllActiveLoans() ?? new List<LibraryLoan>();
    
            if (activeLoans.Count == 0)
            {
                Console.WriteLine("No active loans to return.\n");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
    
            Console.WriteLine("ACTIVE LOANS:\n");
            Console.WriteLine($"{"Loan ID",-25} {"Member Name",-20} {"Book Title",-30} {"Due Date",-12}");
            Console.WriteLine(new string('-', 87));
    
            foreach (var activeloan in activeLoans)
            {
                Console.WriteLine($"{activeloan.RecordId,-25} {activeloan.MemberName,-20} {activeloan.BookTitle,-30} {activeloan.DueDate:yyyy-MM-dd,-12}");
            }
            Console.WriteLine();
            // === END OF ADDED PART ===
    
            Console.Write("Enter Loan ID to return (from list above): ");
            string? id = Console.ReadLine();
    
            // Rest of your code remains the same...
            if (string.IsNullOrWhiteSpace(id))
            {
                Console.WriteLine("Invalid ID!");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }
    
            var loan = loanRepo?.GetLoanById(id);
            if (loan == null)
            {
                Console.WriteLine("Loan not found!");
            }
            else if (loan.IsReturned)
            {
                Console.WriteLine("This book has already been returned!");
            }
            else
            {
                Console.WriteLine($"\nBook: {loan.BookTitle}");
                Console.WriteLine($"Member: {loan.MemberName} (ID: {loan.MemberId})");
                Console.WriteLine($"Due Date: {loan.DueDate:MMMM dd, yyyy}");
                
                Console.Write("\nConfirm return? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    loanRepo?.ReturnBook(id);
                    Console.WriteLine("\nBOOK RETURNED SUCCESSFULLY!");
                }
                else
                {
                    Console.WriteLine("\nReturn cancelled.");
                }
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void UpdateLoan()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("          EXTEND DUE DATE");
            Console.WriteLine("========================================\n");
    
            // === ADD THIS PART: Show active loans first ===
            var activeLoans = loanRepo?.GetAllActiveLoans() ?? new List<LibraryLoan>();
    
            if (activeLoans.Count == 0)
            {
                Console.WriteLine("No active loans to extend.\n");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
    
            Console.WriteLine("ACTIVE LOANS:\n");
            Console.WriteLine($"{"Loan ID",-25} {"Member Name",-20} {"Book Title",-30} {"Due Date",-12}");
            Console.WriteLine(new string('-', 87));
    
            foreach (var activeloan in activeLoans)
            {
            Console.WriteLine($"{activeloan.RecordId,-25} {activeloan.MemberName,-20} {activeloan.BookTitle,-30} {activeloan.DueDate:yyyy-MM-dd,-12}");
            }
            Console.WriteLine();
            // === END OF ADDED PART ===
    
            Console.Write("Enter Loan ID to extend (from list above): ");
            string? id = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(id))
            {
                Console.WriteLine("Invalid ID!");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }
            
            var loan = loanRepo?.GetLoanById(id);
            if (loan == null)
            {
                Console.WriteLine("Loan not found!");
            }
            else if (loan.IsReturned)
            {
                Console.WriteLine("This book has already been returned!");
            }
            else
            {
                Console.WriteLine($"\nBook: {loan.BookTitle}");
                Console.WriteLine($"Member: {loan.MemberName} (ID: {loan.MemberId})");
                Console.WriteLine($"Current Due Date: {loan.DueDate:MMMM dd, yyyy}");
                
                Console.Write("\nNew Due Date (yyyy-mm-dd): ");
                if (DateTime.TryParse(Console.ReadLine(), out DateTime newDueDate))
                {
                    if (newDueDate > loan.DueDate)
                    {
                        loan.DueDate = newDueDate;
                        loanRepo?.UpdateLoan(loan);
                        Console.WriteLine($"\nDue date extended to {newDueDate:MMMM dd, yyyy}!");
                    }
                    else
                    {
                        Console.WriteLine("New due date must be after current due date!");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid date format!");
                }
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void DeleteLoan()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("          SOFT DELETE LOAN");
            Console.WriteLine("========================================\n");
    
            // === ADD THIS PART: Show all loans (including returned?) ===
            var allLoans = loanRepo?.GetAllLoans() ?? new List<LibraryLoan>();
            var activeLoans = allLoans.Where(l => l.IsActive && !l.IsReturned).ToList();
    
            if (activeLoans.Count == 0)
            {
                Console.WriteLine("No loans to archive.\n");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
                }
    
                Console.WriteLine("ACTIVE LOANS:\n");
                Console.WriteLine($"{"Loan ID",-25} {"Member Name",-20} {"Book Title",-30} {"Status",-10}");
                Console.WriteLine(new string('-', 85));
    
                foreach (var activeloan in activeLoans)
            {
            string status = activeloan.DueDate < DateTime.Now ? "OVERDUE" : "ACTIVE";
            Console.WriteLine($"{activeloan.RecordId,-25} {activeloan.MemberName,-20} {activeloan.BookTitle,-30} {status,-10}");
            }
            Console.WriteLine();
            // === END OF ADDED PART ===
    
            Console.Write("Enter Loan ID to archive (from list above): ");
            string? id = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(id))
            {
                Console.WriteLine("Invalid ID!");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }
            
            var loan = loanRepo?.GetLoanById(id);
            if (loan == null)
            {
                Console.WriteLine("Loan not found!");
            }
            else
            {
                Console.WriteLine($"\nBook: {loan.BookTitle}");
                Console.WriteLine($"Member: {loan.MemberName} (ID: {loan.MemberId})");
                Console.Write("\nArchive this loan? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    loanRepo?.SoftDelete(id);
                    Console.WriteLine("\nLoan archived successfully!");
                }
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void HardDeleteLoan()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("       PERMANENT DELETE (DANGER!)");
            Console.WriteLine("========================================\n");
    
            Console.WriteLine("WARNING: This will PERMANENTLY remove the loan record!");
            Console.WriteLine("This action CANNOT be undone!\n");
    
            // === ADD THIS PART: Show all loans ===
            var allLoans = loanRepo?.GetAllLoans() ?? new List<LibraryLoan>();
    
            if (allLoans.Count == 0)
            {
                Console.WriteLine("No loans to delete.\n");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
    
            Console.WriteLine("ALL LOANS (Including returned/archived):\n");
            Console.WriteLine($"{"Loan ID",-25} {"Member Name",-20} {"Book Title",-30} {"Status",-10}");
            Console.WriteLine(new string('-', 85));
    
            foreach (var activeloan in allLoans)
            {
                string status = activeloan.IsReturned ? "RETURNED" : (activeloan.DueDate < DateTime.Now ? "OVERDUE" : "ACTIVE");
                Console.WriteLine($"{activeloan.RecordId,-25} {activeloan.MemberName,-20} {activeloan.BookTitle,-30} {status,-10}");
            }
            Console.WriteLine();
            // === END OF ADDED PART ===
    
            Console.Write("Enter Loan ID to permanently delete (from list above): ");
            string? id = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(id))
            {
                Console.WriteLine("Invalid ID!");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }
            
            var loan = loanRepo?.GetLoanById(id);
            if (loan == null)
            {
                Console.WriteLine("Loan not found!");
            }
            else
            {
                Console.WriteLine($"\nBook: {loan.BookTitle}");
                Console.WriteLine($"Member: {loan.MemberName} (ID: {loan.MemberId})");
                Console.Write("\nType 'CONFIRM' to permanently delete: ");
                if (Console.ReadLine() == "CONFIRM")
                {
                    loanRepo?.HardDelete(id);
                    Console.WriteLine("\nLoan permanently deleted!");
                }
                else
                {
                    Console.WriteLine("\nDeletion cancelled.");
                }
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void GenerateReports()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("========================================");
                Console.WriteLine("          REPORT GENERATOR");
                Console.WriteLine("========================================\n");
                Console.WriteLine("[1] Overdue Books Report");
                Console.WriteLine("[2] Member Loans Report");
                Console.WriteLine("[3] Library Summary Report");
                Console.WriteLine("[4] Back to Main Menu");
                Console.Write("\nChoose report: ");
                
                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        reportGenerator?.GenerateOverdueReport();
                        break;
                    case "2":
                        reportGenerator?.GenerateMemberLoansReport();
                        break;
                    case "3":
                        reportGenerator?.GenerateSummaryReport();
                        break;
                    case "4":
                        back = true;
                        break;
                    default:
                        Console.WriteLine("Invalid option!");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void ViewAuditLog()
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("            AUDIT LOG");
            Console.WriteLine("========================================\n");
            
            try
            {
                if (File.Exists("Data/audit.log"))
                {
                    var logs = File.ReadAllLines("Data/audit.log");
                    int count = Math.Min(30, logs.Length);
                    var recentLogs = logs.Skip(Math.Max(0, logs.Length - 30));
                    
                    Console.WriteLine("RECENT ACTIVITIES:\n");
                    Console.WriteLine(new string('-', 80));
                    
                    foreach (var log in recentLogs)
                    {
                        Console.WriteLine(log);
                    }
                    
                    Console.WriteLine(new string('-', 80));
                    Console.WriteLine($"\nShowing last {count} of {logs.Length} entries");
                }
                else
                {
                    Console.WriteLine("No audit log found yet.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading audit log: {ex.Message}");
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}