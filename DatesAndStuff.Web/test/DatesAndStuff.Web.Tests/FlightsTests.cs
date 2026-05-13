using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Support;
using SeleniumExtras.WaitHelpers;

namespace Flight.Web.Tests
{
	[TestFixture]
	public class FlightTests
	{
		private IWebDriver driver;
		private StringBuilder verificationErrors;
		private const string BaseURL = "https://blazedemo.com/";
		private bool acceptNextAlert = true;

		private Process? _blazorProcess;

		[OneTimeSetUp]
		public void StartBlazorServer()
		{
			var webProjectPath = Path.GetFullPath(Path.Combine(
				Assembly.GetExecutingAssembly().Location,
				"../../../../../../src/DatesAndStuff.Web/DatesAndStuff.Web.csproj"
				));

			var webProjFolderPath = Path.GetDirectoryName(webProjectPath);

			var startInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				//Arguments = $"run --project \"{webProjectPath}\"",
				Arguments = "dotnet run --no-build",
				WorkingDirectory = webProjFolderPath,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			};

			_blazorProcess = Process.Start(startInfo);

			// Wait for the app to become available
			var client = new HttpClient();
			var timeout = TimeSpan.FromSeconds(30);
			var start = DateTime.Now;

			while (DateTime.Now - start < timeout)
			{
				try
				{
					var result = client.GetAsync(BaseURL).Result;
					if (result.IsSuccessStatusCode)
					{
						break;
					}
				}
				catch (Exception e)
				{
					Thread.Sleep(1000);
				}
			}
		}

		[OneTimeTearDown]
		public void StopBlazorServer()
		{
			if (_blazorProcess != null && !_blazorProcess.HasExited)
			{
				_blazorProcess.Kill(true);
				_blazorProcess.Dispose();
			}
		}

		[SetUp]
		public void SetupTest()
		{
			driver = new ChromeDriver();
			verificationErrors = new StringBuilder();
		}

		[TearDown]
		public void TeardownTest()
		{
			try
			{
				driver.Quit();
				driver.Dispose();
			}
			catch (Exception)
			{
				// Ignore errors if unable to close the browser
			}
			Assert.That(verificationErrors.ToString(), Is.EqualTo(""));
		}

		[Test]
		public void Find_Flights_From_MexicoCity_To_Dublin()
		{
			// Arrange
			driver.Navigate().GoToUrl(BaseURL);
			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            // Act
            new SelectElement(wait.Until(ExpectedConditions.ElementToBeClickable(By.Name("fromPort")))).SelectByValue("Mexico City");

            new SelectElement(wait.Until(ExpectedConditions.ElementToBeClickable(By.Name("toPort")))).SelectByValue("Dublin");

            wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//input[@value='Find Flights']"))).Click();

            // Assert
            IReadOnlyCollection<IWebElement> chooseButtons = null!;
            wait.Until(d => {
                try
                {
                    var buttons = d.FindElements(By.XPath("//input[@value='Choose This Flight']"));
					if (buttons.Count == 0) { 
						return false;
					}
                    chooseButtons = buttons;
                    return true;
                }
                catch (NoSuchElementException) {
					return false;
				}
                catch (StaleElementReferenceException) {
					return false;
				}
            });

            chooseButtons.Count.Should().BeGreaterThanOrEqualTo(3);
        }

		private bool IsElementPresent(By by)
		{
			try
			{
				driver.FindElement(by);
				return true;
			}
			catch (NoSuchElementException)
			{
				return false;
			}
		}

		private bool IsAlertPresent()
		{
			try
			{
				driver.SwitchTo().Alert();
				return true;
			}
			catch (NoAlertPresentException)
			{
				return false;
			}
		}

		private string CloseAlertAndGetItsText()
		{
			try
			{
				IAlert alert = driver.SwitchTo().Alert();
				string alertText = alert.Text;
				if (acceptNextAlert)
				{
					alert.Accept();
				}
				else
				{
					alert.Dismiss();
				}
				return alertText;
			}
			finally
			{
				acceptNextAlert = true;
			}
		}
	}
}
