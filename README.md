# Azure Data Lake Analytics Unit Informations

## Abstract

The purpose of this project is to get details on Azure Data Lake Analytics Unit by collecting system informations.

## Implementation

3 types of information is collected:

- **Platform** informations, by using `System.Diagnostics.Environment` properties.
- **Windows Management** informations, by using `System.Management.ManagementObjectSearcher` (code-behind was used for this part).
- **Environment variables**, by using `System.Diagnostics.Environment.GetEnvironmentVariables` collection.

### Output

Each type of information is outputed in a dedicated tab-separated values file:

- au_plateform.tsv
- au_wmi.tsv
- au_env.tsv
- au_process.tsv

### Execution

We select the lowest degree of parallelism (=1), as we are interested in collecting informations for only one instance.

![](medias/submitjob.png).

It takes less than a minute to execute the job.

![](medias/dag.png)

## Informations

### Overview

At the time of script execution (20th of December 2016), one Azure Data Lake Analytics Unit runs on **Microsoft Windows Server 2012 R2 Datacenter** virtual machine, with **18 cores** (AMD64 architecture, clock speed of **2.40GHz**) and **2 GB** of RAM.

U-SQL scripts are probably executed by a process called **scopehost.exe**.

### Platform

| name                   | value                           |
|------------------------|---------------------------------|
| OSVersion              | Microsoft Windows NT 6.2.9200.0 |
| ProcessorCount         | 18                              |
| Is64BitOperatingSystem | True                            |
| MachineName            | MICROSO-KH58PE5                 |
| UserDomainName         | MICROSO-KH58PE5                 |
| UserName               | SandboxUser                     |

### Windows Management

Windows Management informations can be viewed in [au_wmi.tsv](output/au_wmi.md) file.

### Environment Variables

Environment variables can be viewed in [au_env.tsv](output/au_env.md) file.

### Processes

Processes can be viewed in [au_process.tsv](output/au_process.md) file.

## Available Memory

The low amount of reported memory (**2 GB**) is surprising. This is different from what was written in blog post [Understanding the ADL Analytics Unit](https://blogs.msdn.microsoft.com/azuredatalake/2016/10/12/understanding-adl-analytics-unit/):

> Currently, an AU is the equivalent of 2 CPU cores and 6 GB of RAM.

After contacting Microsoft Azure Data Lake Team, I got the following answer (27th of December 2016):

> Your custom code will run inside a VM, so it is sandboxed in a sense and your system libraries should only be able to impact the VM and not the service itself.
> As to the amount of memory, the 6GB of the container will be shared with all your system code, the OS, the compiled vertex code and your custom code.

> When the container initially starts up, it is configured with 2 GB of memory.
> As the user code running within it requests more memory, the container expands to allow more until it reaches the 6 GB limit.
> In your case below, if you allocate some more memory and then query for the physical memory, then you should see a number greater than 2GB.

The U-SQL was then updated to include memory allocation: during script execution, 1 GB of memory is allocated, by chunks of 100 MB.

Allocation actions and potential `OutOfMemoryException` exceptions are collected to feed an output file named au_memorystress.tsv.

This memory stress didn't result in extra-memory allocation as expected (see [au_memorystress.tsv](output/au_memorystress.md)), as script didn't manage to allocate 1 GB (only 800 MB were successfully allocated). I asked again for some details to the Microsoft Azure Data Lake Team (who were kind enough to quickly answer, thanks to them).  I got the following answer (13th of January 2017):

> A single user-defined operator gets its memory limited to about 512 MB, since we currently want to make sure that a vertex (AU) can process parts of the plan that may consist of several UDOs.

> Managed memory for UDO is controlled separately from process memory. UDO has default memory quota of 512 MB. So if your vertex contains 1 UDO it can only expect 512 MB available. There is some additional buffer but it supposed to be used by CLR and runtime code.
