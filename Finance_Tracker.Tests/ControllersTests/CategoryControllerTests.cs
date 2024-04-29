using Finance_Tracker.Controllers;
using Finance_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance_Tracker.Tests.ControllersTests
{
    public class CategoryControllerTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;

        public CategoryControllerTests()
        {
            // Set In-Memory Database
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "FinanceTrackerDbInMemory")
                .Options;
        }

        private ApplicationDbContext CreateContext()
        {
            return new ApplicationDbContext(_dbContextOptions);
        }

        [Fact]
        public void AddOrEdit_WithIdZero_ReturnsViewWithNewCategory()
        {
            // Arrange
            using var context = CreateContext();
            var controller = new CategoryController(context);

            // Act
            var result = controller.AddOrEdit(0);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Category>(viewResult.Model);
            Assert.Equal(0, model.CategoryId);
        }


        [Fact]
        public async Task AddOrEdit_WithValidId_ReturnsViewWithExistingCategory()
        {
            // Arrange
            using var context = CreateContext();
            Console.WriteLine($"Liczba kategorii w bazie danych przed dodaniem: {context.Categories.Count()}");
            context.Categories.Add(new Category { CategoryId = 1, Title = "TestTitle", Icon = "T", Type = "Expense" });
            var testCategoryId = 1;
            var controller = new CategoryController(context);

            // Act
            var result = controller.AddOrEdit(testCategoryId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Category>(viewResult.Model);
            Assert.Equal(testCategoryId, model.CategoryId);
        }


        [Fact]
        public async Task AddOrEdit_WithNewCategory_AddsCategorySuccessfully()
        {
            // Arrange
            using var context = CreateContext();
            var controller = new CategoryController(context);
            var newCategory = new Category {CategoryId = 0, Title = "New Category", Icon = "Icon", Type = "Expense" };

            // Act
            var result = await controller.AddOrEdit(newCategory);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(1, context.Categories.Count());
            Assert.Equal("New Category", context.Categories.First().Title);
        }


        [Fact]
        public async Task AddOrEdit_WithExistingCategory_UpdatesCategorySuccessfully()
        {
            // Arrange
            using var context = CreateContext();
            var existingCategory = new Category { CategoryId = 1, Title = "Existing Category", Icon = "A", Type = "Expense" };
            context.Categories.Add(existingCategory);

            existingCategory.Title = "Updated Category";
            existingCategory.Icon = "B";

            var controller = new CategoryController(context);

            // Act
            var result = await controller.AddOrEdit(existingCategory);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            var category = context.Categories.First();
            Assert.Equal("Updated Category", category.Title);
            Assert.Equal("B", category.Icon);
        }


        [Fact]
        public async Task DeleteConfirmed_WithValidId_DeletesCategorySuccessfully()
        {
            // Arrange
            using var context = CreateContext();
            var categoryIdToDelete = 1; // only category id because this category was already added in the previous test (AddOrEdit_WithExistingCategory_UpdatesCategorySuccessfully)

            //var category = new Category { CategoryId = categoryIdToDelete, Title = "Test Category", Icon = "A", Type = "Expense" };
            //context.Categories.Add(category);
            //context.SaveChanges();

            var controller = new CategoryController(context);

            // Act
            var result = await controller.DeleteConfirmed(categoryIdToDelete);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);

            var deletedCategory = await context.Categories.FindAsync(categoryIdToDelete);
            Assert.Null(deletedCategory);
        }


        [Fact]
        public async Task DeleteConfirmed_WithInvalidId_ReturnsRedirectToAction()
        {
            // Arrange
            using var context = CreateContext();
            var invalidCategoryId = 999;
            var controller = new CategoryController(context);

            // Act
            var result = await controller.DeleteConfirmed(invalidCategoryId);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(CategoryController.Index), redirectToActionResult.ActionName);
        }


    }
}
