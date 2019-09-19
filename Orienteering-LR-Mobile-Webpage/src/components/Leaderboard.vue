<template>
  <div
    class="resultsScreen"
    :style="{ paddingLeft: pageSidePadding + 'px', paddingRight: pageSidePadding + 'px', paddingTop: pageTopPadding + 'px', paddingBottom: pageBottomPadding + 'px' }"
  >
    <b-dropdown id="class-select" v-bind:text="classSelectText" class="m-2">
      <b-dropdown-item-button
        v-for="(cls) in resultsResponse.cmpResults"
        :key="cls.clsId"
        @click="setClass(cls.clsId)"
      >{{cls.clsName}}</b-dropdown-item-button>
    </b-dropdown>

    <b-input-group>
      <vue-autosuggest
        :suggestions="filteredSuggestions"
        @selected="onSelected"
        :limit="10"
        :input-props="searchProps"
        @input="searchInputChange"
        :render-suggestion="renderSuggestion"
        :get-suggestion-value="getSuggestionValue"
      ></vue-autosuggest>
      <b-input-group-append>
        <b-button @click="searchCompetitors">
          <font-awesome-icon icon="search"></font-awesome-icon>
        </b-button>
      </b-input-group-append>
    </b-input-group>

    <table v-if="classSelected && results">
      <tr class="headingRow" :style="{ height: headerRowHeight + 'px' }">
        <th class="className" colspan="3">
          <span class="pillIcon" :style="{ backgroundColor: classColor(results.cls.clsName) }">
            {{ results.cls.clsName }}
            <span
              class="contText"
            >{{ results.continued ? '(Cont...)' : '' }}</span>
            <div class="classMetadata">
              <span
                class="classLength"
              >{{ results.cls.length != null ? formatDistance(results.cls.length) + ' km' : '' }}</span> &#8226;
              <span class="classCourse">{{ results.cls.course }}</span>
            </div>
          </span>
        </th>
        <th class="elapsedHeading" colspan="2">Total</th>
        <th
          class="splitHeading"
          colspan="3"
          v-for="(n, i) in results.cls.radioCount"
          :key="n"
        >Split {{ n }} - {{ results.cls.radioInfo[i].distance != null ? formatDistance(results.cls.radioInfo[i].distance) + ' km' : '' }}</th>
      </tr>
      <tr
        v-for="result of results.cls.clsResults"
        :key="result.id"
        :style="{ height: rowHeight + 'px' }"
        v-bind:class="{ 'proceedToDownload':(result.status == 100) }"
      >
        <td
          class="col-overallRank"
          :style="{ width: colOverallRank + 'px',
                        maxWidth: colOverallRank + 'px' }"
        >
          <font-awesome-icon
            v-if="result.startTime > 0 && result.status == 0 && statusZero(result) == 'running'"
            icon="running"
            class="pillIcon running"
          />
          <font-awesome-icon
            v-else-if="(result.status == 0 && statusZero(result) == 'pending') || (result.startTime <= 0 && result.status == 0)"
            icon="ellipsis-h"
          />
          <template v-else-if="result.status == 1">
            <span class="pillIcon finisher">{{ result.finishRank }}</span>
          </template>
          <template v-else-if="result.status == 100">
            <span class="pillIcon finisher">{{ result.finishRank }}</span>
          </template>
          <template v-else>
            <span class="pillIcon nonfinisher">{{ statusToRank[result.status] }}</span>
          </template>
        </td>

        <td
          class="col-competitor"
          :style="{ width: colCompetitor + 'px',
                        maxWidth: colCompetitor + 'px' }"
        >{{ result.competitor }}</td>
        <td
          class="col-club"
          :style="{ width: colClub + 'px', maxWidth: colClub + 'px' }"
        >{{ result.club }}</td>

        <td
          class="col-elapsedTime"
          :style="{ width: colElapsedTime + 'px',
                        maxWidth: colElapsedTime + 'px' }"
        >
          <template v-if="result.startTime > 0 && competitorStarted(result.startTime) == false">
            <span class="startTimeDisplay">{{ (result.startTime / 10) | formatStartTime }}</span>
          </template>
          <template
            v-else-if="result.startTime > 0 && result.finishTime == null && result.status == 0"
          >{{ (calculateElapsedTime(result.startTime) / 10) | formatAbsoluteTime }}</template>
          <template v-else>{{ result.finishTime }}</template>
        </td>

        <td
          class="col-elapsedDiff"
          :style="{ width: colElapsedDiff + 'px',
                        maxWidth: colElapsedDiff + 'px' }"
        >
          <template v-if="competitorStarted(result.startTime) == false">
            <span class="startTimeDisplay">Start</span>
          </template>
          <template v-else>{{ result.finishDiff }}</template>
        </td>

        <!-- we need to use i (index) rather than n (value) so we start at zero-->
        <template v-for="(n, i) in results.cls.radioCount">
          <flash-cell
            :display-value="result.radios[i].time"
            :watch-value="result.radios[i].time"
            :key="result.id + '-' + result.radios[i].code + '-time'"
            class="col-radioTime"
            :style="{ width: colRadioTime + 'px',
                        maxWidth: colRadioTime + 'px' }"
          ></flash-cell>

          <!-- if no radio punch then print no brackets -->
          <flash-cell
            v-if="result.radios[i].time == null"
            :display-value="null"
            :watch-value="result.radios[i].time"
            :key="result.id + '-' + result.radios[i].code + '-rank'"
            class="col-radioRank"
            :style="{ width: colRadioRank + 'px',
                        maxWidth: colRadioRank + 'px' }"
          ></flash-cell>
          <flash-cell
            v-else
            :display-value="'(' + result.radios[i].rank + ')'"
            :watch-value="result.radios[i].time"
            :key="result.id + '-' + result.radios[i].code + '-rank'"
            class="col-radioRank"
            :style="{ width: colRadioRank + 'px',
                        maxWidth: colRadioRank + 'px' }"
          ></flash-cell>

          <flash-cell
            :display-value="result.radios[i].diff"
            :watch-value="result.radios[i].time"
            :key="result.id + '-' + result.radios[i].code + '-diff'"
            class="col-radioDiff"
            :style="{ width: colRadioDiff + 'px',
                        maxWidth: colRadioDiff + 'px' }"
          ></flash-cell>
        </template>
        <td />
      </tr>
    </table>
  </div>
</template>

<style scoped>
/* roboto-100 - latin */
@font-face {
  font-family: "Roboto";
  font-style: normal;
  font-weight: 100;
  src: local("Roboto Thin"), local("Roboto-Thin"),
    url("/fonts/roboto-v20-latin-100.woff2") format("woff2"),
    /* Chrome 26+, Opera 23+, Firefox 39+ */
      url("/fonts/roboto-v20-latin-100.woff") format("woff"); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
}
/* roboto-100italic - latin */
@font-face {
  font-family: "Roboto";
  font-style: italic;
  font-weight: 100;
  src: local("Roboto Thin Italic"), local("Roboto-ThinItalic"),
    url("/fonts/roboto-v20-latin-100italic.woff2") format("woff2"),
    /* Chrome 26+, Opera 23+, Firefox 39+ */
      url("/fonts/roboto-v20-latin-100italic.woff") format("woff"); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
}
/* roboto-300 - latin */
@font-face {
  font-family: "Roboto";
  font-style: normal;
  font-weight: 300;
  src: local("Roboto Light"), local("Roboto-Light"),
    url("/fonts/roboto-v20-latin-300.woff2") format("woff2"),
    /* Chrome 26+, Opera 23+, Firefox 39+ */
      url("/fonts/roboto-v20-latin-300.woff") format("woff"); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
}
/* roboto-300italic - latin */
@font-face {
  font-family: "Roboto";
  font-style: italic;
  font-weight: 300;
  src: local("Roboto Light Italic"), local("Roboto-LightItalic"),
    url("/fonts/roboto-v20-latin-300italic.woff2") format("woff2"),
    /* Chrome 26+, Opera 23+, Firefox 39+ */
      url("/fonts/roboto-v20-latin-300italic.woff") format("woff"); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
}
/* roboto-regular - latin */
@font-face {
  font-family: "Roboto";
  font-style: normal;
  font-weight: 400;
  src: local("Roboto"), local("Roboto-Regular"),
    url("/fonts/roboto-v20-latin-regular.woff2") format("woff2"),
    /* Chrome 26+, Opera 23+, Firefox 39+ */
      url("/fonts/roboto-v20-latin-regular.woff") format("woff"); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
}
/* roboto-italic - latin */
@font-face {
  font-family: "Roboto";
  font-style: italic;
  font-weight: 400;
  src: local("Roboto Italic"), local("Roboto-Italic"),
    url("/fonts/roboto-v20-latin-italic.woff2") format("woff2"),
    /* Chrome 26+, Opera 23+, Firefox 39+ */
      url("/fonts/roboto-v20-latin-italic.woff") format("woff"); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
}
/* roboto-500 - latin */
@font-face {
  font-family: "Roboto";
  font-style: normal;
  font-weight: 500;
  src: local("Roboto Medium"), local("Roboto-Medium"),
    url("/fonts/roboto-v20-latin-500.woff2") format("woff2"),
    /* Chrome 26+, Opera 23+, Firefox 39+ */
      url("/fonts/roboto-v20-latin-500.woff") format("woff"); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
}
/* roboto-500italic - latin */
@font-face {
  font-family: "Roboto";
  font-style: italic;
  font-weight: 500;
  src: local("Roboto Medium Italic"), local("Roboto-MediumItalic"),
    url("/fonts/roboto-v20-latin-500italic.woff2") format("woff2"),
    /* Chrome 26+, Opera 23+, Firefox 39+ */
      url("/fonts/roboto-v20-latin-500italic.woff") format("woff"); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
}
/* roboto-700 - latin */
@font-face {
  font-family: "Roboto";
  font-style: normal;
  font-weight: 700;
  src: local("Roboto Bold"), local("Roboto-Bold"),
    url("/fonts/roboto-v20-latin-700.woff2") format("woff2"),
    /* Chrome 26+, Opera 23+, Firefox 39+ */
      url("/fonts/roboto-v20-latin-700.woff") format("woff"); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
}
/* roboto-700italic - latin */
@font-face {
  font-family: "Roboto";
  font-style: italic;
  font-weight: 700;
  src: local("Roboto Bold Italic"), local("Roboto-BoldItalic"),
    url("/fonts/roboto-v20-latin-700italic.woff2") format("woff2"),
    /* Chrome 26+, Opera 23+, Firefox 39+ */
      url("/fonts/roboto-v20-latin-700italic.woff") format("woff"); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
}
/* roboto-900 - latin */
@font-face {
  font-family: "Roboto";
  font-style: normal;
  font-weight: 900;
  src: local("Roboto Black"), local("Roboto-Black"),
    url("/fonts/roboto-v20-latin-900.woff2") format("woff2"),
    /* Chrome 26+, Opera 23+, Firefox 39+ */
      url("/fonts/roboto-v20-latin-900.woff") format("woff"); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
}
/* roboto-900italic - latin */
@font-face {
  font-family: "Roboto";
  font-style: italic;
  font-weight: 900;
  src: local("Roboto Black Italic"), local("Roboto-BlackItalic"),
    url("/fonts/roboto-v20-latin-900italic.woff2") format("woff2"),
    /* Chrome 26+, Opera 23+, Firefox 39+ */
      url("/fonts/roboto-v20-latin-900italic.woff") format("woff"); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
}

div.resultsScreen {
  background-color: #333;
  color: #eee;
  font-family: Roboto;
  margin: 0;
}

table {
  border-collapse: collapse;
  white-space: nowrap;
  table-layout: fixed;
  overflow: hidden;
}

table tr td {
  /*border: 1px solid #777;*/
  vertical-align: bottom;
}

td,
th {
  padding: 0 3px;
  box-sizing: border-box;
  position: relative;
}

th {
  border-bottom: 1px solid #777;
  vertical-align: bottom;
  padding-bottom: 10px;
}

@keyframes proceedToDownload {
  0% {
    opacity: 0.1;
  }

  15% {
    opacity: 1;
  }

  25% {
    opacity: 1;
  }

  40% {
    opacity: 0.1;
  }

  100% {
    opacity: 0.1;
  }
}

tr.proceedToDownload td:first-child:after {
  position: absolute;
  z-index: 1;
  display: block;
  text-align: left;
  left: 0;
  top: 0;
  width: 1000px;
  height: 24px;
  padding-top: 5px;
  margin-left: -50px;
  content: "UNOFFICIAL • PROCEED TO DOWNLOAD • UNOFFICIAL • PROCEED TO DOWNLOAD • UNOFFICIAL • PROCEED TO DOWNLOAD";
  font-size: 17px;
  background: repeating-linear-gradient(
    45deg,
    #ef9f01,
    #ef9f01 10px,
    #febb34 10px,
    #febb34 20px
  );
  color: white;
  opacity: 0.1;
  animation-duration: 30000ms;
  animation-name: proceedToDownload;
  animation-iteration-count: infinite;
}

tr:nth-child(even) {
  background-color: #444;
}

tr:nth-child(odd) {
  background-color: #333;
}

th.className {
  text-align: left;
  font-size: 24px;
  text-transform: uppercase;
  padding-top: 10px;
  padding-left: 10px;
  font-weight: 500;
}

tr.headingRow th.className span.pillIcon {
  border-radius: 5px;
  display: inline-block;
  padding: 4px 16px 3px 16px;
  width: 225px;
}

th.className .classMetadata {
  margin-top: 5px;
  font-size: 14px;
  font-weight: 300;
}

th.className .classLength {
  margin-right: 5px;
}

th.className .classCourse {
  margin-left: 5px;
}

th.className .contText {
  font-size: 16px;
}

th.elapsedHeading {
  font-size: 14px;
  text-align: center;
}

th.splitHeading {
  font-size: 14px;
  text-align: center;
}

td {
  border-bottom: 1px solid #777;
}

td.col-overallRank {
  text-align: center;
  font-size: 13px;
  vertical-align: middle;
}

td.col-overallRank .pillIcon {
  border: none;
  padding: 2px 5px;
  color: white;
  text-align: center;
  text-decoration: none;
  display: inline-block;
  margin: 4px 2px;
  border-radius: 18px;
}

td.col-overallRank .pillIcon.finisher {
  background-color: #32a852;
}

td.col-overallRank .pillIcon.running {
  padding: 2px 7px;
  background-color: #d18400;
}

tr.proceedToDownload td.col-overallRank .pillIcon {
  background-color: #d18400;
}

td.col-overallRank .pillIcon.nonfinisher {
  background-color: #8c1414;
}

td.col-competitor {
  max-width: 180px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  vertical-align: middle;
  font-weight: 500;
}

td.col-club {
  vertical-align: middle;
  overflow: hidden;
  text-overflow: ellipsis;
}

td.col-elapsedTime {
  text-align: right;
  vertical-align: middle;
}

td.col-elapsedTime .startTimeDisplay {
  color: #d18400;
  font-weight: 300;
}

td.col-elapsedDiff {
  text-align: left;
  font-size: 12px;
  vertical-align: middle;
  padding-top: 2px;
}

td.col-elapsedDiff .startTimeDisplay {
  color: #d18400;
  font-weight: 300;
}

td.col-radioTime {
  text-align: right;
  vertical-align: middle;
}

td.col-radioRank {
  font-size: 12px;
  vertical-align: middle;
  padding-top: 2px;
}

td.col-radioDiff {
  text-align: left;
  font-size: 12px;
  vertical-align: middle;
  padding-top: 2px;
}

.columns {
  display: flex;
}

.column {
  flex: 0 0 auto;
}
</style>

<script>
// import meosResultsApi from '@/meos-results-api'
import FlashCell from "@/components/FlashCell.vue";
import resultsApiData from "@/leaderboard-data.json";

export default {
  data() {
    return {
      now: new Date(),
      resultsResponse: null,

      windowWidth: 0,
      windowHeight: 0,
      rowHeight: 30,
      headerRowHeight: 80,
      columnGap: 20,
      pageSidePadding: 10,
      pageTopPadding: 0,
      pageBottomPadding: 60,

      // Column widths
      colOverallRank: 40,
      colCompetitor: 180,
      colClub: 50,
      colElapsedTime: 65,
      colElapsedDiff: 45,
      colRadioTime: 65,
      colRadioRank: 30,
      colRadioDiff: 47,

      classSelectText: "Select class to display.",
      classSelected: false,
      selectedClassId: null,
      nameQuery: null,
      queryResult: null,
      searchProps: {
        id: "autosuggest__input",
        placeholder: "Search for competitor by name"
      },
      filteredSuggestions: [],
      selectedCompetitor: null,

      statusToRank: {
        3: "MP",
        4: "DNF",
        5: "DQ",
        6: "OT",
        20: "DNS",
        21: "CNL",
        99: "NP"
      }
    };
  },

  computed: {
    results() {
      const { resultsResponse, classSelected, selectedClassId } = this;
      if (classSelected && resultsResponse != null) {
        return {
          cls: resultsResponse.cmpResults.find(function(cmpResults) {
            return cmpResults.clsId == selectedClassId;
          })
        };
      }
      return null;
    },
    // used for providing suggestions when searching for someone
    searchSuggestions() {
      const { resultsResponse } = this;
      if (resultsResponse) {
        let names = resultsResponse.cmpResults.map(a =>
          a.clsResults.map(b => [b.competitor, a.clsId])
        );
        return [].concat.apply([], names).sort();
      }
      return null;
    }
  },

  filters: {
    formatAbsoluteTime: function(t) {
      if (t) {
        // This code does hh:mm:ss for > 1 hour and mm:ss for < 1 hour

        /*
                    var h, m, s;

                    if (t > 3600) {
                        h = Math.floor(t/3600).toString();
                        m = Math.floor((t/60)%60).toString().padStart(2, '0');
                        s = Math.floor(t%60).toString().padStart(2, '0');
                        return `${h}:${m}:${s}`;
                    }

                    else {
                        if (t >= 600 ) {
                            m = Math.floor((t/60)%60).toString().padStart(2, '0');
                        }
                        else {
                            m = Math.floor((t/60)%60).toString().padStart(1, '0');
                        }
                        s = Math.floor(t%60).toString().padStart(2, '0');
                        return `${m}:${s}`;
                    }
                    */

        // This code does mmm:ss for everyone
        var m, s;

        m = Math.floor(t / 60).toString();
        s = Math.floor(t % 60)
          .toString()
          .padStart(2, "0");
        return `${m}:${s}`;
      }

      return null;
    },

    formatStartTime: function(t) {
      var h, m, s;

      if (t > 3600) {
        h = Math.floor(t / 3600).toString();
        m = Math.floor((t / 60) % 60)
          .toString()
          .padStart(2, "0");
        s = Math.floor(t % 60)
          .toString()
          .padStart(2, "0");
        return `${h}:${m}:${s}`;
      }
    }
  },

  created() {
    window.addEventListener("resize", () => this.updateWindowSize());
    this.updateWindowSize();
    this.refreshResults();

    setInterval(() => (this.now = new Date()), 1000);

    const updateLoop = () => {
      const nowMs = +new Date();
      const updateIntervalMs = 5000;
      const delay = Math.floor(nowMs / 1000) * 1000 - nowMs + updateIntervalMs;

      setTimeout(() => {
        this.refreshResults();
        updateLoop();
      }, delay);
    };

    updateLoop();
  },

  components: {
    FlashCell
  },

  methods: {
    updateWindowSize() {
      this.windowWidth = window.innerWidth;
      this.windowHeight = window.innerHeight;
    },

    async refreshResults() {
      // this.resultsResponse = await meosResultsApi.getResultsScreen()
      this.resultsResponse = resultsApiData;
    },

    // Calculates the current elapsed time for a competitor, based on their startTime
    calculateElapsedTime(startTime) {
      // Time of day in 10ths of seconds
      const { now } = this;
      const currentTimeSecs =
        (now.getSeconds() + 60 * now.getMinutes() + 60 * 60 * now.getHours()) *
        10;

      // Calculate elapsed running time
      const elapsedRunningTime = currentTimeSecs - startTime;

      // Check that it's positive
      if (elapsedRunningTime >= 0) {
        // Return the result
        return elapsedRunningTime;
      }

      // Otherwise, return null
      return null;
    },

    // Returns whether a competitor has started yet or not
    competitorStarted(startTime) {
      // Number of seconds since midnight
      const { now } = this;
      const currentTimeSecs =
        (now.getSeconds() + 60 * now.getMinutes() + 60 * 60 * now.getHours()) *
        10;

      // Calculate elapsed running time
      const elapsedRunningTime = currentTimeSecs - startTime;

      // Check that it's positive
      if (elapsedRunningTime > 0) {
        // Return true
        return true;
      }

      // Otherwise, return false
      return false;
    },

    // Determines when the status is 0 whether the competitor is started or not yet started, and returns the relevant icon
    statusZero(resultObject) {
      // Calculate their elapsed time
      var elapsedRunningTime = this.calculateElapsedTime(
        resultObject.startTime
      );

      // They have not started
      if (elapsedRunningTime == null) {
        // Set the icon to three dots (pending...)
        return "pending";
      }

      // They have started...
      else {
        // Set the icon to a running man
        return "running";
      }
    },

    // Displays the distance info (if available) for a particular radio
    formatDistance(d) {
      // Convert the distance in meters into km for display, rounded to 1dp
      var distanceInKm = parseFloat(d / 1000).toFixed(1);

      // Return the distance
      return distanceInKm;
    },

    // Returns a colour for a given string, based upon the string's hash
    classColor(str) {
      var hash = 0;

      if (str.length === 0) return hash;

      for (var i = 0; i < str.length; i++) {
        hash = str.charCodeAt(i) + ((hash << 5) - hash);
        hash = hash & hash;
      }

      const h = Math.abs(hash) % 360;
      const s = (Math.abs(hash) % 40) + 60;
      const l = (Math.abs(hash) % 25) + 13;

      return `hsl(${h}, ${s}%, ${l}%)`;
    },
    getClassById(clsId) {
      return {
        cls: this.resultsResponse.cmpResults.find(function(cmpResults) {
          return cmpResults.clsId == clsId;
        })
      };
    },
    // sets the displayed class to the one chosen in the dropdown
    setClass(clsId) {
      this.classSelected = false;
      this.classSelectText = this.getClassById(clsId).clsName;
      this.selectedClassId = clsId;
      this.classSelected = true;
    },

    // TODO: this will need swap the class to the one the searched competitor is in
    searchCompetitors() {
      this.queryResult = this.nameQuery;
    },

    onSelected(option) {
      this.selectedCompetitor = option.item[0];
      this.setClass(option.item[1]);
    },
    searchInputChange(text) {
      if (text === "" || text === undefined) {
        return;
      }

      const filteredData = this.searchSuggestions
        .filter(item => {
          return item[0].toLowerCase().indexOf(text.toLowerCase()) > -1;
        })
        .slice(0, this.limit);

      this.filteredSuggestions = [
        {
          data: filteredData
        }
      ];
    },
    renderSuggestion(suggestion) {
      return suggestion.item[0];
    },
    getSuggestionValue(suggestion) {
      return suggestion.item[0];
    }
  }
};
</script>
