# biz.dfch.CS.Entity.LifeCycleManager
[![License](https://img.shields.io/badge/license-Apache%20License%202.0-blue.svg)](https://github.com/dfensgmbh/biz.dfch.CS.Entity.LifeCycleManager/blob/master/LICENSE)

Allows managing the lifecycle of entities. Supports pre- and post-callouts/hooks. Internally the LifeCycleManager uses the [biz.dfch.CS.StateMachine](https://github.com/dfensgmbh/biz.dfch.CS.StateMachine).

## Entity Framework Configuration

The following snippet shows the entity framework configuration on a standalone IIS

  <entityFramework>
    <defaultConnectionFactory type="System.Data.Entity.Infrastructure.SqlConnectionFactory, EntityFramework">
      <parameters>
        <parameter value="Server=.\SQLEXPRESS;User ID=userId;Password=password;Database=Test;" />
      </parameters>
    </defaultConnectionFactory>
    <providers>
      <provider invariantName="System.Data.SqlClient" type="System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer" />
    </providers>
  </entityFramework>
  

## License Information

Telerik JustMock has to be licensed separately. Only the code samples (source code files) are licensed under the Apache 2.0 license. The Telerik JustMock software has to be licensed separately. See the NOTICE file for more information about this.
