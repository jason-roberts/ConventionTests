﻿namespace TestStack.ConventionTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using ApprovalTests;
    using ApprovalTests.Core.Exceptions;
    using TestStack.ConventionTests.Conventions;
    using TestStack.ConventionTests.Internal;

    public static class Convention
    {
        static readonly HtmlReportRenderer HtmlRenderer = new HtmlReportRenderer(AssemblyDirectory);
        static readonly List<ConventionReport> Reports = new List<ConventionReport>();

        public static IEnumerable<ConventionReport> ConventionReports { get { return Reports; } }

        public static void Is<TDataSource, TDataType>(IConvention<TDataSource, TDataType> convention, TDataSource data)
            where TDataSource : IConventionData, ICreateReportLineFor<TDataType>
        {
            Is(convention, data, new ConventionResultExceptionReporter());
        }

        public static void Is<TDataSource, TDataType>(IConvention<TDataSource, TDataType> convention, TDataSource data, IConventionReportRenderer reporter) 
            where TDataSource : IConventionData, ICreateReportLineFor<TDataType>
        {
            try
            {
                var conventionResult = GetConventionReport(convention.ConventionTitle, convention.GetFailingData(data).ToArray(), data);

                Reports.Add(conventionResult);

                new ConventionReportTraceRenderer().Render(conventionResult);
                reporter.Render(conventionResult);
            }
            finally
            {
                HtmlRenderer.Render(Reports.ToArray());
            }
        }

        public static void IsWithApprovedExeptions<TDataSource, TDataType>(IConvention<TDataSource, TDataType> convention, TDataSource data)
            where TDataSource : IConventionData, ICreateReportLineFor<TDataType>
        {
            var conventionResult = GetConventionReport(convention.ConventionTitle, convention.GetFailingData(data).ToArray(), data);
            Reports.Add(conventionResult);

            try
            {
                var conventionReportTextRenderer = new ConventionReportTextRenderer();
                conventionReportTextRenderer.RenderItems(conventionResult);
                conventionResult.WithApprovedException(conventionReportTextRenderer.Output);

                conventionReportTextRenderer.Render(conventionResult);
                Approvals.Verify(conventionReportTextRenderer.Output);

                new ConventionReportTraceRenderer().Render(conventionResult);
            }
            catch (ApprovalException ex)
            {
                throw new ConventionFailedException("Approved exceptions for convention differs\r\n\r\n"+ex.Message, ex);
            }
            finally
            {
                HtmlRenderer.Render(Reports.ToArray());
            }
        }

        public static void Is<TDataSource, TDataType>(ISymmetricConvention<TDataSource, TDataType> convention, TDataSource data)
            where TDataSource : IConventionData, ICreateReportLineFor<TDataType>
        {
            Is(convention, data, new ConventionResultExceptionReporter());
        }

        public static void Is<TDataSource, TDataType>(ISymmetricConvention<TDataSource, TDataType> convention, TDataSource data, IConventionReportRenderer reporter)
            where TDataSource : IConventionData, ICreateReportLineFor<TDataType>
        {
            try
            {
                var conventionResult = GetConventionReport(convention.ConventionTitle, convention.GetFailingData(data).ToArray(), data);
                var inverseConventionResult = GetConventionReport(convention.InverseTitle, convention.GetFailingInverseData(data).ToArray(), data);

                Reports.Add(conventionResult);
                Reports.Add(inverseConventionResult);

                new ConventionReportTraceRenderer().Render(conventionResult, inverseConventionResult);
                reporter.Render(conventionResult, inverseConventionResult);
            }
            finally
            {
                HtmlRenderer.Render(Reports.ToArray());
            }
        }

        public static void IsWithApprovedExeptions<TDataSource, TDataType>(ISymmetricConvention<TDataSource, TDataType> convention, TDataSource data)
            where TDataSource : IConventionData, ICreateReportLineFor<TDataType>
        {
            var conventionResult = GetConventionReport(convention.ConventionTitle, convention.GetFailingData(data).ToArray(), data);
            var inverseConventionResult = GetConventionReport(convention.InverseTitle, convention.GetFailingInverseData(data).ToArray(), data);
            Reports.Add(conventionResult);
            Reports.Add(inverseConventionResult);

            try
            {
                var conventionReportTextRenderer = new ConventionReportTextRenderer();
                // Add approved exceptions to report
                conventionReportTextRenderer.RenderItems(conventionResult);
                conventionResult.WithApprovedException(conventionReportTextRenderer.Output);

                // Add approved exceptions to inverse report
                conventionReportTextRenderer.RenderItems(inverseConventionResult);
                inverseConventionResult.WithApprovedException(conventionReportTextRenderer.Output);

                //Render both, with approved exceptions included
                conventionReportTextRenderer.Render(conventionResult, inverseConventionResult);
                Approvals.Verify(conventionReportTextRenderer.Output);
                
                // Trace on success
                new ConventionReportTraceRenderer().Render(conventionResult, inverseConventionResult);
            }
            catch (ApprovalException ex)
            {
                throw new ConventionFailedException("Approved exceptions for convention differs\r\n\r\n" + ex.Message, ex);
            }
            finally
            {
                HtmlRenderer.Render(Reports.ToArray());
            }
        }

        static ConventionReport GetConventionReport<TDataSource, TDataType>(string conventionTitle, TDataType[] failingData, TDataSource data)
            where TDataSource : IConventionData, ICreateReportLineFor<TDataType>
        {
            data.EnsureHasNonEmptySource();
            var passed = failingData.None();

            var conventionResult = new ConventionReport(
                passed ? Result.Passed : Result.Failed,
                conventionTitle,
                data.Description,
                failingData.Select(data.CreateReportLine));
            return conventionResult;
        }

        // http://stackoverflow.com/questions/52797/c-how-do-i-get-the-path-of-the-assembly-the-code-is-in#answer-283917
        static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}