using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CrmMida.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260731183000_AddProspects")]
public partial class AddProspects;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260731184500_AddOpportunitiesAndActivities")]
public partial class AddOpportunitiesAndActivities;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260731190000_AddQuotes")]
public partial class AddQuotes;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260731203000_AddCatalogItems")]
public partial class AddCatalogItems;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260731214500_AddQuoteDeliveryAndPublicAccess")]
public partial class AddQuoteDeliveryAndPublicAccess;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260731223000_AddLicensesAndRenewals")]
public partial class AddLicensesAndRenewals;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260731224000_AddLicenseAlertDispatches")]
public partial class AddLicenseAlertDispatches;
