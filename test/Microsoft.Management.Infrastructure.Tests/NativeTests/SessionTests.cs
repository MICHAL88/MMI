﻿using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Management.Infrastructure.Native;
using MMI.Tests;
using Xunit;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;

namespace MMI.Tests.Native
{
    public class SessionTests : NativeTestsBase
    {
#if !_LINUX
        private const string TestEnumerateInstanceNamespace = "root/cimv2";
        private const string TestEnumerateInstanceClassName = "Win32_ComputerSystem";
        private const string TestEnumerateInstanceStringPropertyName = "Name";
        private const MI_Flags TestEnumerateInstanceStringPropertyFlags = MI_Flags.MI_FLAG_KEY | MI_Flags.MI_FLAG_PROPERTY | MI_Flags.MI_FLAG_NOT_MODIFIED | MI_Flags.MI_FLAG_READONLY;
        private string TestEnumerateInstanceStringPropertyValue = Environment.MachineName;
        
        private const string TestGetClassNamespace = "root/cimv2";
        private const string TestGetClassClassName = "Win32_ComputerSystem";
        private const string TestGetClassUUID = "{8502C4B0-5FBB-11D2-AAC1-006008C78BC7}";
        private const string TestGetClassPropertyName = "Name";
        private const MI_Flags TestGetClassPropertyFlags = MI_Flags.MI_FLAG_READONLY | MI_Flags.MI_FLAG_NULL | MI_Flags.MI_FLAG_PROPERTY | MI_Flags.MI_FLAG_EXTENDED;
        private string TestGetClassMethodName = "SetPowerState";
        private uint TestGetClassParameterCount = 2u;
        private string TestGetClassParameterName = "PowerState";
        private MI_Type TestGetClassParameterType = MI_Type.MI_UINT16;
        private uint TestGetClassParameterIndex = 0;
#else
        private const string TestEnumerateInstanceNamespace = "root/test";
        private const string TestEnumerateInstanceClassName = "TestClass_AllDMTFTypes";
        private const string TestEnumerateInstanceStringPropertyName = "v_string";
        private const MI_Flags TestEnumerateInstanceStringPropertyFlags = MI_Flags.MI_FLAG_PROPERTY | MI_Flags.MI_FLAG_NOT_MODIFIED | MI_Flags.MI_FLAG_BORROW;
        private string TestEnumerateInstanceStringPropertyValue = "TestString 0";
        
        private const string TestGetClassNamespace = "test/c";
        private const string TestGetClassClassName = "MSFT_Person";
        private const string TestGetClassUUID = "{8502C4B0-5FBB-11D2-AAC1-006008C78BC7}";
        private const string TestGetClassPropertyName = "First";
        private const MI_Flags TestGetClassPropertyFlags = MI_Flags.MI_FLAG_PROPERTY | MI_Flags.MI_FLAG_BORROW;
        private string TestGetClassMethodName = "TestAllTypes";
        private uint TestGetClassParameterCount = 3u;
        private string TestGetClassParameterName = "Z";
        private MI_Type TestGetClassParameterType = MI_Type.MI_REAL32;
        private uint TestGetClassParameterIndex = 2;
#endif

        [Fact]
        public void TestSessionPositive()
        {
            MI_Operation operation = null;
            this.Session.TestConnection(MI_OperationFlags.Default, null, out operation);

            bool moreResults;
            MI_Result result;
            string errorMessage = null;
            MI_Instance instance = null;
            MI_Instance errorDetails = null;
            var res = operation.GetInstance(out instance, out moreResults, out result, out errorMessage, out errorDetails);
            MIAssert.Succeeded(res, "Expect GetInstance result to succeed");
            MIAssert.Succeeded(result, "Expect actual operation result to be success");

            res = operation.Close();
            MIAssert.Succeeded(res, "Expect to be able to close completed operation");
        }
        
        [Fact]
        public void SimpleEnumerateInstance()
        {
            MI_Operation operation = null;
            this.Session.EnumerateInstances(MI_OperationFlags.MI_OPERATIONFLAGS_DEFAULT_RTTI,
                MI_OperationOptions.Null,
                TestEnumerateInstanceNamespace,
                TestEnumerateInstanceClassName,
                false,
                null,
                out operation);

            bool moreResults = true;
            MI_Instance clonedInstance = null;

            MI_Result secondaryResult;
            string errorMessage = null;
            MI_Instance instanceOut = null;
            MI_Instance errorDetails = null;
            var res = operation.GetInstance(out instanceOut, out moreResults, out secondaryResult, out errorMessage, out errorDetails);
            MIAssert.Succeeded(res, "Expect the first GetInstance call to succeed");
            MIAssert.Succeeded(secondaryResult, "Expect the logical result of the GetInstance call to succeed");

            if (!instanceOut.IsNull)
            {
                res = instanceOut.Clone(out clonedInstance);
                MIAssert.Succeeded(res, "Expect the clone to succeed");
            }

            while (moreResults)
            {
                res = operation.GetInstance(out instanceOut, out moreResults, out secondaryResult, out errorMessage, out errorDetails);
                MIAssert.Succeeded(res, "Expect GetInstance to succeed even if we don't want the result");
                MIAssert.Succeeded(secondaryResult, "Expect the logical result of the GetInstance call to succeed even if we don't want the result");
            }

            res = operation.Close();
            MIAssert.Succeeded(res, "Expect operation to close successfully");

            string className = null;
            res = clonedInstance.GetClassName(out className);
            MIAssert.Succeeded(res, "Expect GetClassName to succeed");
            Assert.Equal(TestEnumerateInstanceClassName, className, "Expect the class name to be the one we queried");

            MI_Value elementValue = null;
            MI_Type elementType;
            MI_Flags elementFlags;
            UInt32 elementIndex;
            res = clonedInstance.GetElement(TestEnumerateInstanceStringPropertyName, out elementValue, out elementType, out elementFlags, out elementIndex);
            MIAssert.Succeeded(res, "Expect GetElement to succeed");

            Assert.Equal(MI_Type.MI_STRING, elementType, "Expect that the property is registered as a string");
            Assert.Equal(TestEnumerateInstanceStringPropertyFlags,
                elementFlags, "Expect the element flags to also be properly available from the query");
            Assert.Equal(TestEnumerateInstanceStringPropertyValue, elementValue.String, "Expect the machine name to have survived the whole journey");
        }

        [WindowsFact]
        public void SimpleGetClass()
        {
            MI_Operation operation = null;
            this.Session.GetClass(MI_OperationFlags.Default, MI_OperationOptions.Null, TestGetClassNamespace, TestGetClassClassName, null, out operation);

            MI_Class classOut;
            MI_Class clonedClass = null;
            MI_Result result;
            string errorMessage = null;
            MI_Instance errorDetails = null;
            bool moreResults = false;
            var res = operation.GetClass(out classOut, out moreResults, out result, out errorMessage, out errorDetails);
            MIAssert.Succeeded(res, "Expect the first GetClass call to succeed");

            Assert.True(!classOut.IsNull, "Expect retrieved class instance to be non-null");
            res = classOut.Clone(out clonedClass);
            MIAssert.Succeeded(res, "Expect the clone to succeed");

            while (moreResults)
            {
                MI_Result secondaryResult = MI_Result.MI_RESULT_OK;
                res = operation.GetClass(out classOut, out moreResults, out secondaryResult, out errorMessage, out errorDetails);
                MIAssert.Succeeded(res, "Expect GetClass to succeed even if we don't want the result");
            }

            res = operation.Close();
            MIAssert.Succeeded(res, "Expect operation to close successfully");

            MI_Value elementValue;
            UInt32 elementIndex;
            MI_Flags elementFlags;
            MI_Type elementType;
            bool valueExists;
            string referenceClass;
            MI_QualifierSet propertyQualifierSet;
            res = clonedClass.GetElement(TestGetClassPropertyName, out elementValue, out valueExists, out elementType, out referenceClass, out propertyQualifierSet, out elementFlags, out elementIndex);
            MIAssert.Succeeded(res, "Expect to see the normal property on the class");
            Assert.Equal(MI_Type.MI_STRING, elementType, "Expect the CIM property to have the right type");
            Assert.Equal(TestGetClassPropertyFlags, elementFlags, "Expect the CIM property to have the normal flags");

            MI_Type miClassQualifierType;
            MI_Value miClassQualifierValue;
            MI_Flags miClassQualifierFlags;
            MI_QualifierSet classQualifierSet;
            res = clonedClass.GetClassQualifierSet(out classQualifierSet);
            MIAssert.Succeeded(res, "Expect to be able to get class qualifiers set");

            UInt32 qualifierIndex;
            res = classQualifierSet.GetQualifier("UUID", out miClassQualifierType, out miClassQualifierFlags, out miClassQualifierValue, out qualifierIndex);
            MIAssert.Succeeded(res, "Expect to be able to get qualifier information from class");

            Assert.Equal(MI_Type.MI_STRING, miClassQualifierType, "Expect qualifier type to be a string");
            Assert.True((miClassQualifierFlags & MI_Flags.MI_FLAG_ENABLEOVERRIDE) != 0, "Expect flags to be standard flags");
            Assert.Equal(TestGetClassUUID, miClassQualifierValue.String, "Expect UUID of class to be the known UUID");

            MI_ParameterSet parameters;
            MI_QualifierSet methodQualifierSet;
            UInt32 methodIndex;
            res = clonedClass.GetMethod(TestGetClassMethodName, out methodQualifierSet, out parameters, out methodIndex);
            MIAssert.Succeeded(res, "Expect to be able to get method from class");

            UInt32 parameterCount;
            res = parameters.GetParameterCount(out parameterCount);
            MIAssert.Succeeded(res, "Expect to be able to get count from parameter set");
            Assert.Equal(TestGetClassParameterCount, parameterCount, "Expect there to be the documented number of parameters");

            MI_Type parameterType;
            string parameterReferenceClass;
            MI_QualifierSet parameterQualifierSet;
            UInt32 parameterIndex;
            res = parameters.GetParameter(TestGetClassParameterName, out parameterType, out parameterReferenceClass, out parameterQualifierSet, out parameterIndex);
            MIAssert.Succeeded(res, "Expect to be able to get parameter from parameter set");

            Assert.Equal(TestGetClassParameterType, parameterType, "Expect parameter type to be the documented type");
            Assert.Equal(TestGetClassParameterIndex, parameterIndex, "Expect the power state to be the first parameter");
        }

        [WindowsFact]
        public void ClassHashCode_Sanity()
        {
            MI_Operation cimClassOperation1;
            MMI.Tests.Native.StaticFixtures.Session.GetClass(MI_OperationFlags.Default, null, TestGetClassNamespace, TestGetClassClassName, null, out cimClassOperation1);
            MI_Class cimClass1;
            bool moreResults1;
            MI_Result operationRes1;
            MI_Instance completionDetails1;
            string errorMessage1;
            var res1 = cimClassOperation1.GetClass(out cimClass1, out moreResults1, out operationRes1, out errorMessage1, out completionDetails1);
            MIAssert.Succeeded(res1);
            MIAssert.Succeeded(operationRes1);
            Assert.False(moreResults1, "Expect no more results after getting named class");

            MI_Operation cimClassOperation2;
            MMI.Tests.Native.StaticFixtures.Session.GetClass(MI_OperationFlags.Default, null, TestGetClassNamespace, TestGetClassClassName, null, out cimClassOperation2);
            MI_Class cimClass2;
            bool moreResults2;
            MI_Result operationRes2;
            MI_Instance completionDetails2;
            string errorMessage2;
            var res2 = cimClassOperation2.GetClass(out cimClass2, out moreResults2, out operationRes2, out errorMessage2, out completionDetails2);
            MIAssert.Succeeded(res2);
            MIAssert.Succeeded(operationRes2);
            Assert.False(moreResults2, "Expect no more results after getting named class");

            int hash1 = cimClass1.GetClassHashCode();
            int hash2 = cimClass2.GetClassHashCode();

            Assert.Equal(hash1, hash2, "MI_Class HashCodes are not identical for two objects of the same class.");

            cimClassOperation1.Close();
            cimClassOperation2.Close();
        }

        [WindowsFact]
        public void CredentialTest()
        {
            string testpassword = "mypassword";
            System.Security.SecureString testSS = new System.Security.SecureString();
            foreach (char ch in testpassword)
                testSS.AppendChar(ch);

            CimCredential c = new CimCredential(PasswordAuthenticationMechanism.Default, "myhost", "johnkord", testSS);
            CimSessionOptions csOpts = new CimSessionOptions();
            csOpts.AddDestinationCredentials(c);
            CimSession s = CimSession.Create("localhost", csOpts);

            
        }
    }
}
