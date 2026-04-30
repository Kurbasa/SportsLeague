using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SportsLeague.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Founded = table.Column<int>(type: "integer", nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CoachName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HomeTeamId = table.Column<int>(type: "integer", nullable: false),
                    AwayTeamId = table.Column<int>(type: "integer", nullable: false),
                    MatchDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HomeScore = table.Column<int>(type: "integer", nullable: false),
                    AwayScore = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Venue = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    JerseyNumber = table.Column<int>(type: "integer", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AwayTeamId_MatchDate",
                table: "Matches",
                columns: new[] { "AwayTeamId", "MatchDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeTeamId_MatchDate",
                table: "Matches",
                columns: new[] { "HomeTeamId", "MatchDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId_JerseyNumber",
                table: "Players",
                columns: new[] { "TeamId", "JerseyNumber" },
                unique: true);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION ensure_match_business_rules()
                RETURNS trigger AS $$
                DECLARE
                    match_day date;
                    conflict_exists boolean;
                BEGIN
                    IF NEW."HomeTeamId" = NEW."AwayTeamId" THEN
                        RAISE EXCEPTION 'Team cannot play against itself.';
                    END IF;

                    match_day := (NEW."MatchDate" AT TIME ZONE 'UTC')::date;

                    SELECT EXISTS (
                        SELECT 1
                        FROM "Matches" m
                        WHERE m."Id" <> COALESCE(NEW."Id", -1)
                          AND ((m."MatchDate" AT TIME ZONE 'UTC')::date) = match_day
                          AND (
                               m."HomeTeamId" = NEW."HomeTeamId"
                            OR m."AwayTeamId" = NEW."HomeTeamId"
                            OR m."HomeTeamId" = NEW."AwayTeamId"
                            OR m."AwayTeamId" = NEW."AwayTeamId"
                          )
                    ) INTO conflict_exists;

                    IF conflict_exists THEN
                        RAISE EXCEPTION 'Schedule conflict: a team already has a match on this day.';
                    END IF;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_matches_business_rules
                BEFORE INSERT OR UPDATE ON "Matches"
                FOR EACH ROW
                EXECUTE FUNCTION ensure_match_business_rules();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_matches_business_rules ON "Matches";
                DROP FUNCTION IF EXISTS ensure_match_business_rules();
                """);

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
