/**
 * Copyright 2015 Marc Rufer, d-fens GmbH
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    [TestClass]
    public class JobsControllerTest
    {
        [TestMethod]
        public void GetJobsForUserWithReadPermissionReturnsHisJobs()
        {
            
        }

        [TestMethod]
        public void GetJobsForUserWithoutReadPermissionReturnsForbidden()
        {
            
        }

        [TestMethod]
        public void GetJobsWithNonExistingJobsForCurrentUserReturnsEmptyList()
        {

        }

        [TestMethod]
        public void GetJobsWithFilterOnStateReturnsJobsWithDesiredState()
        {

        }

        [TestMethod]
        public void GetJobByIdForUserWithOwnershipAndReadPermissionReturnsDesiredJob()
        {
            
        }

        [TestMethod]
        public void GetJobByIdForUserWithoutOwnershipReturnsForbidden()
        {

        }

        [TestMethod]
        public void GetJobByIdForNonExistingJobIdReturnsNotFound()
        {

        }

        [TestMethod]
        public void GetJobByIdForUserWithoutReadPermissionReturnsForbidden()
        {

        }

        [TestMethod]
        public void PutJobForUserWithUpdatePermissionAndOwnershipUpdatesJob()
        {
            
        }

        [TestMethod]
        public void PutJobForUserWithoutUpdatePermissionReturnsForbidden()
        {

        }

        [TestMethod]
        public void PutJobForUserWithoutOwnershipReturnsForbidden()
        {

        }

        [TestMethod]
        public void PutJobForAuthorizedUserSetsUpdatedDate()
        {

        }

        [TestMethod]
        public void PutJobForNonExistingJobIdReturnsNotFound()
        {

        }

        [TestMethod]
        public void PostJobForUserWithCreatePermissionAndOwnershipCreatesJob()
        {

        }

        [TestMethod]
        public void PutJobForUserWithoutWritePermissionReturnsForbidden()
        {

        }

        [TestMethod]
        public void PostJobForAuthorizedUserSetsStateTypeAndCreatedDate()
        {

        }

        [TestMethod]
        public void PostJobForNonExistingJobIdReturnsNotFound()
        {

        }

        [TestMethod]
        public void PatchJobForUserWithUpdatePermissionAndOwnershipUpdatesDeliveredFields()
        {

        }

        [TestMethod]
        public void PatchJobForUserWithoutUpdatePermissionReturnsForbidden()
        {

        }

        [TestMethod]
        public void PatchJobForUserWithoutOwnershipReturnsForbidden()
        {

        }

        [TestMethod]
        public void PatchJobForAuthorizedUserSetsUpdatedDate()
        {

        }

        [TestMethod]
        public void PatchForNonExistingJobIdReturnsNotFound()
        {

        }

        [TestMethod]
        public void DeleteJobForUserWithDeletePermissionAndOwnershipDeletesJob()
        {

        }

        [TestMethod]
        public void DeleteJobForUserWithoutUpdatePermissionReturnsForbidden()
        {

        }

        [TestMethod]
        public void DeleteJobForUserWithoutOwnershipReturnsForbidden()
        {

        }

        [TestMethod]
        public void DeleteForNonExistingJobIdReturnsNotFound()
        {
            
        }
    }
}
