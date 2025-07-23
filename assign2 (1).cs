using System;

class ProductProcessor
{
    static void Main()
    {
        string productId,name;
        int qty;
        decimal unitPrice,totalAmount,discount,finalAmount;
        
        // Step 1: Read product details from user
        Console.WriteLine("Enter Product ID: ");
        productId = Console.ReadLine();

        Console.WriteLine("Enter Product Name: ");
        name = Console.ReadLine();

        Console.WriteLine("Enter Quantity: ");
        qty = int.Parse(Console.ReadLine());

        Console.WriteLine("Enter Unit Price: ");
        unitPrice = decimal.Parse(Console.ReadLine());

        // Step 2: Calculate total amount
        totalAmount = qty * unitPrice;

        // Step 3: Apply 10% discount
        discount = totalAmount * 0.10M;
        finalAmount = totalAmount - discount;

        // Step 4: Display all details
        Console.WriteLine($"Product ID      : {productId}");
        Console.WriteLine($"Product Name    : {name}");
        Console.WriteLine($"Quantity        : {qty}");
        Console.WriteLine($"Unit Price      : ₹{unitPrice}");
        Console.WriteLine($"Total Amount    : ₹{totalAmount}");
        Console.WriteLine($"Discount (10%)  : ₹{discount}");
        Console.WriteLine($"Final Amount    : ₹{finalAmount}");
        Console.ReadLine();
        
    }
}
