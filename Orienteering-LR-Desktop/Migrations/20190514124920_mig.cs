using Microsoft.EntityFrameworkCore.Migrations;

namespace Orienteering_LR_Desktop.Migrations
{
    public partial class mig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clubs",
                columns: table => new
                {
                    ClubId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubs", x => x.ClubId);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    CourseId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Distance = table.Column<float>(nullable: false),
                    Climb = table.Column<float>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    CourseData = table.Column<string>(nullable: true),
                    DistanceData = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.CourseId);
                });

            migrationBuilder.CreateTable(
                name: "Punches",
                columns: table => new
                {
                    PunchId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChipId = table.Column<int>(nullable: false),
                    Stage = table.Column<string>(nullable: true),
                    CheckpointId = table.Column<int>(nullable: false),
                    Timestamp = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Punches", x => x.PunchId);
                });

            migrationBuilder.CreateTable(
                name: "RaceClasses",
                columns: table => new
                {
                    RaceClassId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Abbreviation = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    AgeFrom = table.Column<int>(nullable: false),
                    AgeTo = table.Column<int>(nullable: false),
                    Gender = table.Column<string>(nullable: true),
                    _RaceClassTypeValue = table.Column<int>(nullable: false),
                    RaceClassType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceClasses", x => x.RaceClassId);
                });

            migrationBuilder.CreateTable(
                name: "Stages",
                columns: table => new
                {
                    Name = table.Column<string>(nullable: false),
                    Current = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stages", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "ClassCourse",
                columns: table => new
                {
                    RaceClassId = table.Column<int>(nullable: false),
                    CourseId = table.Column<int>(nullable: false),
                    CompetitionPos = table.Column<int>(nullable: false),
                    Stage = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassCourse", x => new { x.RaceClassId, x.CourseId });
                    table.ForeignKey(
                        name: "FK_ClassCourse_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Competitors",
                columns: table => new
                {
                    CompetitorId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    Age = table.Column<int>(nullable: false),
                    StartNo = table.Column<int>(nullable: false),
                    Gender = table.Column<string>(nullable: true),
                    ChipId = table.Column<int>(nullable: false),
                    ClubId = table.Column<int>(nullable: false),
                    RaceClassId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competitors", x => x.CompetitorId);
                    table.ForeignKey(
                        name: "FK_Competitors_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "ClubId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Competitors_RaceClasses_RaceClassId",
                        column: x => x.RaceClassId,
                        principalTable: "RaceClasses",
                        principalColumn: "RaceClassId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompTimes",
                columns: table => new
                {
                    CompetitorId = table.Column<int>(nullable: false),
                    Stage = table.Column<string>(nullable: false),
                    ChipId = table.Column<int>(nullable: false),
                    Times = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompTimes", x => new { x.CompetitorId, x.Stage });
                    table.ForeignKey(
                        name: "FK_CompTimes_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "CompetitorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    TeamId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompetitorPos = table.Column<int>(nullable: false),
                    CompetitorId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.TeamId);
                    table.ForeignKey(
                        name: "FK_Teams_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "CompetitorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassCourse_CourseId",
                table: "ClassCourse",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Competitors_ClubId",
                table: "Competitors",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Competitors_RaceClassId",
                table: "Competitors",
                column: "RaceClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CompetitorId",
                table: "Teams",
                column: "CompetitorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassCourse");

            migrationBuilder.DropTable(
                name: "CompTimes");

            migrationBuilder.DropTable(
                name: "Punches");

            migrationBuilder.DropTable(
                name: "Stages");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Competitors");

            migrationBuilder.DropTable(
                name: "Clubs");

            migrationBuilder.DropTable(
                name: "RaceClasses");
        }
    }
}
