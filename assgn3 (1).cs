using System;

class ProductProcessor
{
    static void Main()
    {
        // Static product values for online IDE testing
        string productId = "P1001";
        string name = "Notebook";
        int qty = 10;
        decimal unitPrice = 75.00M;

        decimal totalAmount = qty * unitPrice;
        decimal discount = totalAmount * 0.10M;
        decimal finalAmount = totalAmount - discount;

        Console.WriteLine("--- Product Summary ---");
        Console.WriteLine($"Product ID      : {productId}");
        Console.WriteLine($"Product Name    : {name}");
        Console.WriteLine($"Quantity        : {qty}");
        Console.WriteLine($"Unit Price      : ₹{unitPrice.ToString("F2")}");
        Console.WriteLine($"Total Amount    : ₹{totalAmount.ToString("F2")}");
        Console.WriteLine($"Discount (10%)  : ₹{discount.ToString("F2")}");
        Console.WriteLine($"Final Amount    : ₹{finalAmount.ToString("F2")}");
    }
}