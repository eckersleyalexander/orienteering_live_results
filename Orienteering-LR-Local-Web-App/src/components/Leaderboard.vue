<template>
    <div class="leaderboard">
        <b-table
            :items="data"
            :fields="tableFields"
            :busy.sync="loading"
            dark
            borderless
        >
            <template slot="Position" slot-scope="data">
                {{ data.index + 1 }}
            </template>

            <template slot="Name" slot-scope="data">
                {{ data.item.FirstName }} {{ data.item.LastName }}
            </template>

            <template slot="Status" slot-scope="data">
                {{ data.item.Times.length ? "Started" : "Ready" }}
            </template>
        </b-table>
    </div>
</template>

<script>
export default {
    name: "Leaderboard",
    data() {
        return {
            loading: true,
            data: null,
            tableFields: null,
            staticFields: ["Position", "Name", "Status"],
            bestTimes: []
        };
    },
    created() {
        this.$options.sockets.onmessage = data => this.onmessage(data);
    },
    methods: {
        onmessage: function(msg) {
            this.loading = true;
            this.data = JSON.parse(msg.data);

            // generate table fields
            var dynamicFields = [];
            if (this.data[0].Times.length) {
                for (var i = 1; i <= this.data[0].Times.length - 1; i++) {
                    dynamicFields.push("Leg " + i);
                    dynamicFields.push("Diff " + i);

                    // put the biggest time possible in each date
                    this.bestTimes[i] = new Date(8640000000000000);
                }
            }
            this.tableFields = this.staticFields.concat(dynamicFields);

            // assign legs
            for (i = 0; i <= this.data.length - 1; i++) {
                if (this.data[i].Times.length) {
                    for (var j = 1; j <= this.data[i].Times.length - 1; j++) {
                        var t = new Date(this.data[i].Times[j]);
                        if (t < this.bestTimes[j]) {
                            this.bestTimes[j] = t;
                        }
                        this.data[i]["Leg " + j] = t
                            .toISOString()
                            .slice(11, -2);
                    }
                }
            }

            // calculate time differences
            for (i = 0; i <= this.data.length - 1; i++) {
                if (this.data[i].Times.length) {
                    for (j = 1; j <= this.data[i].Times.length - 1; j++) {
                        t = new Date(this.data[i].Times[j]);

                        // calculate time diff
                        if (t.getTime() !== this.bestTimes[j].getTime()) {
                            if (t < this.bestTimes[j]) {
                                this.data[i]["Diff " + j] =
                                    "(-" +
                                    new Date(t - this.bestTimes[j])
                                        .toISOString()
                                        .slice(11, -2) +
                                    ")";
                            } else {
                                this.data[i]["Diff " + j] =
                                    "(+" +
                                    new Date(t - this.bestTimes[j])
                                        .toISOString()
                                        .slice(11, -2) +
                                    ")";
                            }
                        } else {
                            this.data[i]["Diff " + j] = "( - )";
                        }
                    }
                }
            }
            this.loading = false;
        }
    }
};
</script>
