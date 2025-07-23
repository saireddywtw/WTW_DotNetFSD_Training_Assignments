using System;

class EmployeeDetails
{
    static void Main()
    {
        // Declare variables to store employee details
        int empno, deptno;
        string ename, job;
        decimal salary;

        // Read employee details from the user
        Console.WriteLine("Enter Employee Number: ");
        empno = int.Parse(Console.ReadLine());

        Console.WriteLine("Enter Employee Name: ");
        ename = Console.ReadLine();

        Console.WriteLine("Enter Job Title: ");
        job = Console.ReadLine();

        Console.WriteLine("Enter Salary: ");
        salary = decimal.Parse(Console.ReadLine());

        Console.WriteLine("Enter Department Number: ");
        deptno = int.Parse(Console.ReadLine());

        Console.WriteLine($"Employee Details: EmpNo: {empno}, Name: {ename}, Job: {job}, Salary: {salary}, DeptNo: {deptno}");
        Console.ReadLine();
    }
}
